using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.AspNetCore.Rest.Serialization;
using NCoreUtils.Data;

namespace NCoreUtils.AspNetCore.Rest.Internal;

public abstract class UpdateInvoker
{
    protected sealed class RestUpdateInvocation<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TData, TId>(
        IRestUpdate<TData, TId> invoker,
        IRestUpdateContext<TData, TId> context)
        : RestMethodInvocation<TData>
        where TData : class, IHasId<TId>
    {
        private IRestUpdate<TData, TId> Invoker { get; } = invoker ?? throw new ArgumentNullException(nameof(invoker));

        private readonly ArgumentCollection<IRestUpdateContext<TData, TId>> Args = ArgumentCollection.Create(context);

        public override Type ItemType => typeof(TData);

        public override object Instance => Invoker;

        public override IReadOnlyList<object> Arguments => Args;

        public override ValueTask<TData> InvokeAsync(CancellationToken cancellationToken = default)
            => Invoker.InvokeAsync(Args.Arg, cancellationToken);

        public override RestMethodInvocation<TData> UpdateArguments(IReadOnlyList<object> arguments)
        {
            if (arguments.Count != 1)
            {
                throw new InvalidOperationException("Invalid number of arguments.");
            }
            return new RestUpdateInvocation<TData, TId>(Invoker, (IRestUpdateContext<TData, TId>)arguments[0]);
        }
    }

    internal UpdateInvoker() { }

    public abstract ValueTask Invoke(HttpContext httpContext, IRestContextMetadata metadata, object id, CancellationToken cancellationToken);
}

public sealed class UpdateInvoker<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TData, TId>(
    IServiceProvider serviceProvider,
    RestAccessConfiguration accessConfiguration,
    IRestMethodInvoker? methodInvoker = default,
    IRestUpdate<TData, TId>? implementation = default,
    IDeserializer<TData>? deserializer = default) : UpdateInvoker
    where TData : class, IHasId<TId>
{
    private IServiceProvider ServiceProvider { get; } = serviceProvider;

    private RestAccessConfiguration AccessConfiguration { get; } = accessConfiguration;

    private IRestMethodInvoker MethodInvoker { get; } = methodInvoker ?? DefaultRestMethodInvoker.Instance;

    private IRestUpdate<TData, TId> Implementation { get; } = implementation ?? ActivatorUtilities.CreateInstance<DefaultRestUpdate<TData, TId>>(serviceProvider);

    private IDeserializer<TData> Deserializer { get; } = deserializer ?? serviceProvider.GetOrCreateDeserializer<TData>();

    public override async ValueTask Invoke(HttpContext httpContext, IRestContextMetadata metadata, object id, CancellationToken cancellationToken)
    {
        var accessValidator = AccessConfiguration.Update.GetOrCreateValidator(ServiceProvider, out var disposeValidator);
        try
        {
            (await accessValidator.ValidateAsync(httpContext.User, cancellationToken)).ThrowOnFailure();
            var data = await Deserializer.DeserializeAsync(httpContext.Request.Body, cancellationToken);
            var context = RestContext.Update((TId)id, data, metadata);
            var invocation = new RestUpdateInvocation<TData, TId>(Implementation, context);
            await MethodInvoker.InvokeAsync(invocation, cancellationToken);
            httpContext.Response.StatusCode = 204;
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