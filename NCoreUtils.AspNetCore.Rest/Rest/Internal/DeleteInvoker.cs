using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.Data;

namespace NCoreUtils.AspNetCore.Rest.Internal;

public abstract class DeleteInvoker
{
    protected sealed class RestDeleteInvocation<TData, TId>(IRestDelete<TData, TId> invoker, IRestDeleteContext<TData, TId> context)
        : ViodRestMethodInvocation
        where TData : IHasId<TId>
    {
        private readonly ArgumentCollection<IRestDeleteContext<TData, TId>> Args = ArgumentCollection.Create(context);

        private IRestDelete<TData, TId> Invoker { get; } = invoker;

        public override Type ItemType => typeof(TData);

        public override object Instance => Invoker;

        public override IReadOnlyList<object> Arguments => Args;

        public override ValueTask InvokeAsync(CancellationToken cancellationToken = default)
            => Invoker.InvokeAsync(Args.Arg, cancellationToken);

        public override ViodRestMethodInvocation UpdateArguments(IReadOnlyList<object> arguments)
        {
            if (arguments.Count != 1)
            {
                throw new InvalidOperationException("Invalid number of arguments.");
            }
            return new RestDeleteInvocation<TData, TId>(Invoker, (IRestDeleteContext<TData, TId>)arguments[0]);
        }
    }

    internal DeleteInvoker() { }

    public abstract ValueTask Invoke(HttpContext httpContext, IRestContextMetadata metadata, object id, bool force, CancellationToken cancellationToken);
}

public sealed class DeleteInvoker<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TData, TId>(
    IServiceProvider serviceProvider,
    RestAccessConfiguration accessConfiguration,
    IRestMethodInvoker? methodInvoker = default,
    IRestDelete<TData, TId>? implementation = default) : DeleteInvoker
    where TData : IHasId<TId>
{
    private IServiceProvider ServiceProvider { get; } = serviceProvider;

    private RestAccessConfiguration AccessConfiguration { get; } = accessConfiguration;

    private IRestMethodInvoker MethodInvoker { get; } = methodInvoker ?? DefaultRestMethodInvoker.Instance;

    private IRestDelete<TData, TId> Implementation { get; } = implementation ?? ActivatorUtilities.CreateInstance<DefaultRestDelete<TData, TId>>(serviceProvider);

    public override async ValueTask Invoke(HttpContext httpContext, IRestContextMetadata metadata, object id, bool force, CancellationToken cancellationToken)
    {
        var accessValidator = AccessConfiguration.Delete.GetOrCreateValidator(ServiceProvider, out var disposeValidator);
        try
        {
            (await accessValidator.ValidateAsync(httpContext.User, cancellationToken).ConfigureAwait(false)).ThrowOnFailure();
            var context = RestContext.Delete<TData, TId>((TId)id, force, metadata);
            var invocation = new RestDeleteInvocation<TData, TId>(Implementation, context);
            // using (var activity = G.ActivitySource.StartActivity("REST DELETE method execution"))
            {
                await MethodInvoker.InvokeAsync(invocation, cancellationToken).ConfigureAwait(false);
            }
            httpContext.Response.StatusCode = 200;
        }
        finally
        {
            if (disposeValidator)
            {
                await G.DisposeAsync(accessValidator);
            }
        }
    }
}