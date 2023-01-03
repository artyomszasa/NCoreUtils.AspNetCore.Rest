using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.AspNetCore.Rest.Serialization;
using NCoreUtils.Data;

namespace NCoreUtils.AspNetCore.Rest.Internal
{
    public abstract class ItemInvoker
    {
        protected sealed class RestItemInvocation<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.Interfaces)] TData, TId> : RestMethodInvocation<TData>
            where TData : IHasId<TId>
        {
            private readonly IRestItem<TData, TId> _invoker;

            private readonly ArgumentCollection<TId, AsyncQueryFilter> _args;

            public override Type ItemType => typeof(TData);

            public override object Instance => _invoker;

            public override IReadOnlyList<object> Arguments => _args;

            public RestItemInvocation(IRestItem<TData, TId> invoker, TId id, AsyncQueryFilter filter)
            {
                _invoker = invoker;
                _args = new ArgumentCollection<TId, AsyncQueryFilter>(id, filter);
            }

            public override ValueTask<TData> InvokeAsync(CancellationToken cancellationToken = default)
                => _invoker.InvokeAsync(_args.Arg1, _args.Arg2, cancellationToken);

            public override RestMethodInvocation<TData> UpdateArguments(IReadOnlyList<object> arguments)
            {
                if (arguments.Count != 2)
                {
                    throw new InvalidOperationException("Invalid number of arguments.");
                }
                return new RestItemInvocation<TData, TId>(_invoker, (TId)arguments[0], (AsyncQueryFilter)arguments[1]);
            }
        }

        internal ItemInvoker() { }

        public abstract ValueTask Invoke(HttpContext httpContext, object id, CancellationToken cancellationToken);
    }

    public sealed class ItemInvoker<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.Interfaces)] TData, TId> : ItemInvoker
        where TData : class, IHasId<TId>
    {
        readonly IServiceProvider _serviceProvider;

        readonly RestAccessConfiguration _accessConfiguration;

        readonly IRestMethodInvoker _methodInvoker;

        readonly IRestItem<TData, TId> _implementation;

        ISerializer<TData> _serializer;

        public ItemInvoker(
            IServiceProvider serviceProvider,
            RestAccessConfiguration accessConfiguration,
            IRestMethodInvoker? methodInvoker = default,
            IRestItem<TData, TId>? implmentation = default,
            ISerializerFactory? serializerFactory = default)
        {
            _serviceProvider = serviceProvider;
            _accessConfiguration = accessConfiguration;
            _methodInvoker = methodInvoker ?? DefaultRestMethodInvoker.Instance;
            _implementation = implmentation ?? ActivatorUtilities.CreateInstance<DefaultRestItem<TData, TId>>(serviceProvider);
            _serializer = (serializerFactory ?? JsonSerializerContextSerializerFactory.Create(serviceProvider))
                .GetSerializer<TData>();
        }

        public override async ValueTask Invoke(HttpContext httpContext, object id, CancellationToken cancellationToken)
        {
            var accessValidator = _accessConfiguration.Query.CreateValidator(_serviceProvider, out var disposeValidator);
            try
            {
                if (!await accessValidator.ValidateAsync(httpContext.User, cancellationToken))
                {
                    throw new UnauthorizedException();
                }
                var filter = null != accessValidator && accessValidator is IQueryAccessValidator queryAccessValidator
                    ? new AsyncQueryFilter((source, ctoken) => queryAccessValidator.FilterQueryAsync(source, httpContext.User, ctoken))
                    : ListInvoker._noFilter;
                var invocation = new RestItemInvocation<TData, TId>(_implementation, (TId)id, filter);
                var result = await _methodInvoker.InvokeAsync(invocation, cancellationToken);
                if (result is null)
                {
                    httpContext.Response.StatusCode = 404;
                    return;
                }
                await _serializer.SerializeAsync(new HttpResponseOutput(httpContext.Response), result, cancellationToken);
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
}