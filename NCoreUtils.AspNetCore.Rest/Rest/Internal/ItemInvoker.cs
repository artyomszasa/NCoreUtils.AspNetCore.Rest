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

public abstract class ItemInvoker
{
    protected sealed class RestItemInvocation<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.Interfaces)] TData, TId>(
        IRestItem<TData, TId> invoker,
        IRestItemContext<TData, TId> context)
        : RestMethodInvocation<TData?>
        where TData : IHasId<TId>
    {
        private readonly ArgumentCollection<IRestItemContext<TData, TId>> Args = ArgumentCollection.Create(context);

        private IRestItem<TData, TId> Invoker { get; } = invoker;

        public override Type ItemType => typeof(TData);

        public override object Instance => Invoker;

        public override IReadOnlyList<object> Arguments => Args;

        public override ValueTask<TData?> InvokeAsync(CancellationToken cancellationToken = default)
            => Invoker.InvokeAsync(Args.Arg, cancellationToken);

        public override RestMethodInvocation<TData?> UpdateArguments(IReadOnlyList<object> arguments)
        {
            if (arguments.ThrowIfNull().Count != 1)
            {
                throw new InvalidOperationException("Invalid number of arguments.");
            }
            return new RestItemInvocation<TData, TId>(Invoker, (IRestItemContext<TData, TId>)arguments[0]);
        }
    }

    internal ItemInvoker() { }

    public abstract ValueTask Invoke(HttpContext httpContext, IRestContextMetadata metadata, object id, CancellationToken cancellationToken);
}

public sealed class ItemInvoker<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.Interfaces)] TData, TId>(
    IServiceProvider serviceProvider,
    RestAccessConfiguration accessConfiguration,
    IRestMethodInvoker? methodInvoker = default,
    IRestItem<TData, TId>? implementation = default,
    ISerializerFactory? serializerFactory = default) : ItemInvoker
    where TData : class, IHasId<TId>
{
    private IServiceProvider ServiceProvider { get; } = serviceProvider;

    private RestAccessConfiguration AccessConfiguration { get; } = accessConfiguration;

    private IRestMethodInvoker MethodInvoker { get; } = methodInvoker ?? DefaultRestMethodInvoker.Instance;

    private IRestItem<TData, TId> Implementation { get; } = implementation ?? ActivatorUtilities.CreateInstance<DefaultRestItem<TData, TId>>(serviceProvider);

    private ISerializer<TData> Serializer { get; } = (serializerFactory ?? JsonTypeInfoSerializerFactory.Create(serviceProvider))
            .GetSerializer<TData>();

    public override async ValueTask Invoke(HttpContext httpContext, IRestContextMetadata metadata, object id, CancellationToken cancellationToken)
    {
        var accessValidator = AccessConfiguration.Query.GetOrCreateValidator(ServiceProvider, out var disposeValidator);
        try
        {
            (await accessValidator.ValidateAsync(httpContext.User, cancellationToken).ConfigureAwait(false)).ThrowOnFailure();
            var filter = null != accessValidator && accessValidator is IQueryAccessStatusValidator queryAccessValidator
                ? new AsyncQueryFilter((source, ctoken) => queryAccessValidator.FilterQueryAsync(source, httpContext.User, ctoken))
                : ListInvoker._noFilter;
            var context = RestContext.Item<TData, TId>((TId)id, filter, metadata);
            var invocation = new RestItemInvocation<TData, TId>(Implementation, context);
            TData? result;
            // using (var activity = G.ActivitySource.StartActivity("REST ITEM method execution"))
            {
                result = await MethodInvoker.InvokeAsync(invocation, cancellationToken).ConfigureAwait(false);
            }
            if (result is null)
            {
                httpContext.Response.StatusCode = 404;
                return;
            }
            // using (var activity = G.ActivitySource.StartActivity("REST ITEM method result serialization"))
            {
                await Serializer.SerializeAsync(new HttpResponseOutput(httpContext.Response), result, cancellationToken)
                    .ConfigureAwait(false);
            }
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