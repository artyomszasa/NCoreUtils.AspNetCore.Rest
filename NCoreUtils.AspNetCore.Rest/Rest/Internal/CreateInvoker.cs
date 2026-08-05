using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.AspNetCore.Rest.Serialization;
using NCoreUtils.Data;

namespace NCoreUtils.AspNetCore.Rest.Internal;

public abstract class CreateInvoker
{
    protected sealed class RestCreateInvocation<TData, TId>(IRestCreate<TData, TId> invoker, IRestCreateContext<TData, TId> context)
        : RestMethodInvocation<TData>
        where TData : notnull, IHasId<TId>
    {
        private readonly ArgumentCollection<IRestCreateContext<TData, TId>> Args = ArgumentCollection.Create(context);

        private IRestCreate<TData, TId> Invoker { get; } = invoker;

        public override Type ItemType => typeof(TData);

        public override object Instance => Invoker;

        public override IReadOnlyList<object> Arguments => Args;

        public override ValueTask<TData> InvokeAsync(CancellationToken cancellationToken = default)
            => Invoker.InvokeAsync(Args.Arg, cancellationToken);

        public override RestMethodInvocation<TData> UpdateArguments(IReadOnlyList<object> arguments)
        {
            Preconditions.ThrowIfNull(arguments);
            if (arguments.Count != 1)
            {
                throw new InvalidOperationException("Invalid number of arguments.");
            }
            return new RestCreateInvocation<TData, TId>(Invoker, (IRestCreateContext<TData, TId>)arguments[0]);
        }
    }

    internal CreateInvoker() { }

    public abstract ValueTask Invoke(HttpContext httpContext, IRestContextMetadata metadata, CancellationToken cancellationToken);
}

public sealed class CreateInvoker<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TData, TId>(
    IServiceProvider serviceProvider,
    RestAccessConfiguration accessConfiguration,
    IRestMethodInvoker? methodInvoker = default,
    IRestCreate<TData, TId>? implementation = default)
    : CreateInvoker
    where TData : notnull, IHasId<TId>
{
    private IServiceProvider ServiceProvider { get; } = serviceProvider;

    private RestAccessConfiguration AccessConfiguration { get; } = accessConfiguration;

    private IRestMethodInvoker MethodInvoker { get; } = methodInvoker ?? DefaultRestMethodInvoker.Instance;

    private IRestCreate<TData, TId> Implementation { get; } = implementation ?? ActivatorUtilities.CreateInstance<DefaultRestCreate<TData, TId>>(serviceProvider);

    private IDeserializer<TData> Deserializer { get; } = serviceProvider.GetOrCreateDeserializer<TData>();

    private static Uri CreateItemUri(HttpContext httpContext, TId id)
    {
        // TODO: generate location based on configuration instead...
        var request = httpContext.Request;
        var builder = new UriBuilder
        {
            Scheme = request.Scheme,
            Path = request.Path.ToUriComponent()
        };
        if (request.Host.HasValue)
        {
            builder.Host = request.Host.Host;
            if (request.Host.Port.HasValue)
            {
                var port = request.Host.Port.Value;
                if (!((request.IsHttps && port == 443) || (!request.IsHttps && port == 80)))
                {
                    builder.Port = port;
                }
            }
        }
        else
        {
            builder.Host = "127.0.0.1";
        }
        var idPart = id is IConvertible convertible
            ? convertible.ToString(CultureInfo.InvariantCulture)
            : id is IFormattable formattable
                ? formattable.ToString(default, CultureInfo.InvariantCulture)
                : id!.ToString();
        if (builder.Path.EndsWith('/'))
        {
            builder.Path += idPart;
        }
        else
        {
            builder.Path += $"/{idPart}";
        }
        return builder.Uri;
    }

    public override async ValueTask Invoke(HttpContext httpContext, IRestContextMetadata metadata, CancellationToken cancellationToken)
    {
        var accessValidator = AccessConfiguration.Create.GetOrCreateValidator(ServiceProvider, out var disposeValidator);
        try
        {
            (await accessValidator.ValidateAsync(httpContext.User, cancellationToken).ConfigureAwait(false)).ThrowOnFailure();
            TData data;
            // using (var activity = G.ActivitySource.StartActivity("REST CREATE method input deserialization"))
            {
                data = await Deserializer.DeserializeAsync(httpContext.Request.Body, cancellationToken).ConfigureAwait(false);
            }
            var context = RestContext.Create<TData, TId>(data, metadata);
            var invocation = new RestCreateInvocation<TData, TId>(Implementation, context);
            TData result;
            // using (var activity = G.ActivitySource.StartActivity("REST CREATE method execution"))
            {
                result = await MethodInvoker.InvokeAsync(invocation, cancellationToken).ConfigureAwait(false);
            }
            httpContext.Response.Headers.Append("Location", CreateItemUri(httpContext, result.Id).AbsoluteUri);
            httpContext.Response.StatusCode = 201;
        }
        finally
        {
            if (disposeValidator)
            {
                await G.DisposeAsync(accessValidator).ConfigureAwait(false);
            }
        }
    }
}