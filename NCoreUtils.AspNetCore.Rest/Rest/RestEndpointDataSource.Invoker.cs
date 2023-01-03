using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.AspNetCore.Rest.Internal;
using NCoreUtils.Data;

namespace NCoreUtils.AspNetCore.Rest
{
    public partial class RestEndpointDataSource
    {
        private abstract class Invoker
        {
            private static readonly ConcurrentDictionary<Type, Invoker> _invokerCache = new ConcurrentDictionary<Type, Invoker>();

            private static readonly Func<Type, Invoker> _invokerFactory;

            [UnconditionalSuppressMessage("Trimming", "IL2111", Justification = "Should be preserved at registration.")]
            static Invoker()
            {
                _invokerFactory = CreateInvoker;
            }

            [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Preserved by caller.")]
            [UnconditionalSuppressMessage("Trimming", "IL2055", Justification = "Preserved by caller.")]
            [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Preserved during registration.")]
            private static Invoker CreateInvoker([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type entityType)
            {
                if (NCoreUtils.Data.IdUtils.TryGetIdType(entityType, out var idType))
                {
                    return (Invoker)Activator.CreateInstance(typeof(Invoker<,>).MakeGenericType(entityType, idType), true)!;
                }
                throw new InvalidOperationException($"{entityType} does not implement IHasId interface.");
            }

            [UnconditionalSuppressMessage("Trimming", "IL2111", Justification = "Should be preserved at registration.")]
            [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Invoker<,>))]
            [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ListInvoker<>))]
            public static Task InvokeList(Type entityType, HttpContext httpContext, RestAccessConfiguration accessConfiguration)
                => _invokerCache.GetOrAdd(entityType, _invokerFactory)
                    .InvokeList(httpContext, accessConfiguration);

            [UnconditionalSuppressMessage("Trimming", "IL2111", Justification = "Should be preserved at registration.")]
            [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Invoker<,>))]
            [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ItemInvoker<,>))]
            public static Task InvokeItem(Type entityType, HttpContext httpContext, object id, RestAccessConfiguration accessConfiguration)
                => _invokerCache.GetOrAdd(entityType, _invokerFactory)
                    .InvokeItem(httpContext, id, accessConfiguration);

            [UnconditionalSuppressMessage("Trimming", "IL2111", Justification = "Should be preserved at registration.")]
            [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Invoker<,>))]
            [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CreateInvoker<,>))]
            public static Task InvokeCreate(Type entityType, HttpContext httpContext, RestAccessConfiguration accessConfiguration)
                => _invokerCache.GetOrAdd(entityType, _invokerFactory)
                    .InvokeCreate(httpContext, accessConfiguration);

            [UnconditionalSuppressMessage("Trimming", "IL2111", Justification = "Should be preserved at registration.")]
            [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Invoker<,>))]
            [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UpdateInvoker<,>))]
            public static Task InvokeUpdate(Type entityType, HttpContext httpContext, object id, RestAccessConfiguration accessConfiguration)
                => _invokerCache.GetOrAdd(entityType, _invokerFactory)
                    .InvokeUpdate(httpContext, id, accessConfiguration);

            [UnconditionalSuppressMessage("Trimming", "IL2111", Justification = "Should be preserved at registration.")]
            [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Invoker<,>))]
            [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DeleteInvoker<,>))]
            public static Task InvokeDelete(Type entityType, HttpContext httpContext, object id, bool force, RestAccessConfiguration accessConfiguration)
                => _invokerCache.GetOrAdd(entityType, _invokerFactory)
                    .InvokeDelete(httpContext, id, force, accessConfiguration);

            [UnconditionalSuppressMessage("Trimming", "IL2111", Justification = "Should be preserved at registration.")]
            [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Invoker<,>))]
            [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ReductionInvoker<>))]
            public static Task InvokeReduction(Type entityType, HttpContext httpContext, string reduction, RestAccessConfiguration accessConfiguration)
                => _invokerCache.GetOrAdd(entityType, _invokerFactory)
                    .InvokeReduction(httpContext, reduction, accessConfiguration);

            protected abstract Task InvokeList(HttpContext httpContext, RestAccessConfiguration accessConfiguration);

            protected abstract Task InvokeItem(HttpContext httpContext, object id, RestAccessConfiguration accessConfiguration);

            protected abstract Task InvokeCreate(HttpContext httpContext, RestAccessConfiguration accessConfiguration);

            protected abstract Task InvokeUpdate(HttpContext httpContext, object id, RestAccessConfiguration accessConfiguration);

            protected abstract Task InvokeDelete(HttpContext httpContext, object id, bool force, RestAccessConfiguration accessConfiguration);

            protected abstract Task InvokeReduction(HttpContext httpContext, string reduction, RestAccessConfiguration accessConfiguration);
        }

        private sealed class Invoker<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TData, TId> : Invoker
            where TData : class, IHasId<TId>
        {
            protected override Task InvokeCreate(HttpContext httpContext, RestAccessConfiguration accessConfiguration)
                => ActivatorUtilities.CreateInstance<CreateInvoker<TData, TId>>(httpContext.RequestServices, accessConfiguration)
                    .Invoke(httpContext, httpContext.RequestAborted)
                    .AsTask();

            protected override Task InvokeDelete(HttpContext httpContext, object id, bool force, RestAccessConfiguration accessConfiguration)
                => ActivatorUtilities.CreateInstance<DeleteInvoker<TData, TId>>(httpContext.RequestServices, accessConfiguration)
                    .Invoke(httpContext, id, force, httpContext.RequestAborted)
                    .AsTask();

            protected override Task InvokeItem(HttpContext httpContext, object id, RestAccessConfiguration accessConfiguration)
                => ActivatorUtilities.CreateInstance<ItemInvoker<TData, TId>>(httpContext.RequestServices, accessConfiguration)
                    .Invoke(httpContext, id, httpContext.RequestAborted)
                    .AsTask();

            protected override Task InvokeList(HttpContext httpContext, RestAccessConfiguration accessConfiguration)
                => ActivatorUtilities.CreateInstance<ListInvoker<TData>>(httpContext.RequestServices, accessConfiguration)
                    .Invoke(httpContext, httpContext.RequestAborted)
                    .AsTask();

            protected override Task InvokeReduction(HttpContext httpContext, string reduction, RestAccessConfiguration accessConfiguration)
                => ActivatorUtilities.CreateInstance<ReductionInvoker<TData>>(httpContext.RequestServices, accessConfiguration)
                    .Invoke(httpContext, reduction, httpContext.RequestAborted)
                    .AsTask();

            protected override Task InvokeUpdate(HttpContext httpContext, object id, RestAccessConfiguration accessConfiguration)
                => ActivatorUtilities.CreateInstance<UpdateInvoker<TData, TId>>(httpContext.RequestServices, accessConfiguration)
                    .Invoke(httpContext, id, httpContext.RequestAborted)
                    .AsTask();
        }
    }
}