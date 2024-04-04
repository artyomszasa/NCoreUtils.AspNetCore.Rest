using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NCoreUtils.AspNetCore.Rest.Serialization;

namespace NCoreUtils.AspNetCore.Rest.Internal;

public abstract class ListInvoker
{
    internal static readonly AsyncQueryFilter _noFilter = static (source, _) => new ValueTask<System.Linq.IQueryable>(source);

    protected sealed class RestCollectionInvocation<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(
        IRestListCollection<T> instance,
        IRestListCollectionContext<T> context)
        : RestMethodEnumerableInvocation<T>
    {
        private readonly ArgumentCollection<IRestListCollectionContext<T>> Args = ArgumentCollection.Create(context);

        private IRestListCollection<T> Invoker { get; } = instance;

        public override Type ItemType => typeof(T);

        public override object Instance => Invoker;

        public override IReadOnlyList<object> Arguments => Args;

        public override IAsyncEnumerable<T> InvokeAsync(CancellationToken cancellationToken)
            => Invoker.InvokeAsync(Args.Arg, cancellationToken);

        public override RestMethodEnumerableInvocation<T> UpdateArguments(IReadOnlyList<object> arguments)
        {
            if (arguments.Count != 1)
            {
                throw new InvalidOperationException("Invalid number of arguments.");
            }
            return new RestCollectionInvocation<T>(Invoker, (IRestListCollectionContext<T>)arguments[0]);
        }
    }

    internal ListInvoker() { }

    public abstract ValueTask Invoke(HttpContext httpContext, IRestContextMetadata metadata, CancellationToken cancellationToken);
}

public sealed class ListInvoker<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TData, TId>(
    IServiceProvider serviceProvider,
    RestAccessConfiguration accessConfiguration,
    ILogger<ListInvoker> logger,
    IRestQueryParser? queryParser = null,
    IRestMethodInvoker? methodInvoker = null,
    IRestListCollection<TData>? implementation = null,
    ISerializerFactory? serializerFactory = default) : ListInvoker
{
    private static readonly string Type = typeof(TData).Name;

    private IServiceProvider ServiceProvider { get; } = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    private RestAccessConfiguration AccessConfiguration { get; } = accessConfiguration ?? throw new ArgumentNullException(nameof(accessConfiguration));

    private IRestQueryParser QueryParser { get; } = queryParser ?? new DefaultRestQueryParser();

    private IRestMethodInvoker MethodInvoker { get; } = methodInvoker ?? DefaultRestMethodInvoker.Instance;

    private IRestListCollection<TData> Implementation { get; } = implementation ?? ActivatorUtilities.CreateInstance<DefaultRestListCollection<TData>>(serviceProvider);

    private ISerializerFactory SerializerFactory { get; } = serializerFactory ?? JsonTypeInfoSerializerFactory.Create(serviceProvider);

    private ILogger Logger { get; } = logger ?? throw new ArgumentNullException(nameof(logger));

    private async ValueTask DoInvoke(
        IRestContextMetadata metadata,
        RestQuery restQuery,
        AsyncQueryFilter filter,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var context = RestContext.ListCollection<TData, TId>(restQuery, filter, metadata);
        var invocation = new RestCollectionInvocation<TData>(Implementation, context);
        var result = MethodInvoker.InvokeAsync(invocation, cancellationToken);
        var serializer = SerializerFactory.GetSerializer<IAsyncEnumerable<TData>>();
        await serializer.SerializeAsync(new HttpResponseOutput(response), result, cancellationToken)
            .ConfigureAwait(false);
    }

    public override async ValueTask Invoke(HttpContext context, IRestContextMetadata metadata, CancellationToken cancellationToken)
    {
        var accessValidator = AccessConfiguration.Query.GetOrCreateValidator(ServiceProvider, out var disposeValidator);
        try
        {
            var validationResult = await accessValidator.ValidateAsync(context.User, cancellationToken);
            //Logger.LogTrace("[{Type}] Access validation ({AccessAllowed}).", Type, validationResult.Success);
            Logger.LogRestEntityAccessValidation(Type, validationResult.Success);
            validationResult.ThrowOnFailure();
            var filter = null != accessValidator && accessValidator is IQueryAccessStatusValidator queryAccessValidator
                ? new AsyncQueryFilter((source, ctoken) => queryAccessValidator.FilterQueryAsync(source, context.User, ctoken))
                : _noFilter;
            using var restQuery = await QueryParser.ParseAsync(context.Request, cancellationToken);
            Logger.LogRestQueryParsingDone(Type);
            if (!restQuery.Fields.HasValue || restQuery.Fields.Value.Count == 0)
            {
                await DoInvoke(metadata, restQuery, filter, context.Response, cancellationToken);
            }
            else
            {
                // FIXME: implement
                throw new NotImplementedException("WIP");
            }
        }
        finally
        {
            if (disposeValidator)
            {
                (accessValidator as IDisposable)?.Dispose();
            }
        }
    }
}