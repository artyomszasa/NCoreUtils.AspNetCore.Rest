using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.Data;

namespace NCoreUtils.AspNetCore.Rest.Internal
{
    public abstract class UpdateInvoker
    {
        protected sealed class RestUpdateInvocation<TData, TId> : RestMethodInvocation<TData>
            where TData : class, IHasId<TId>
        {
            readonly IRestUpdate<TData, TId> _invoker;

            readonly ArgumentCollection<TId, TData> _args;

            public override Type ItemType => typeof(TData);

            public override object Instance => _invoker;

            public override IReadOnlyList<object> Arguments => _args;

            public RestUpdateInvocation(IRestUpdate<TData, TId> invoker, TId id, TData data)
            {
                _invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
                _args = new ArgumentCollection<TId, TData>(
                    id,
                    data ?? throw new ArgumentNullException(nameof(data))
                );
            }

            public override ValueTask<TData> InvokeAsync(CancellationToken cancellationToken = default)
                => _invoker.InvokeAsync(_args.Arg1, _args.Arg2, cancellationToken);

            public override RestMethodInvocation<TData> UpdateArguments(IReadOnlyList<object> arguments)
            {
                if (arguments.Count != 2)
                {
                    throw new InvalidOperationException("Invalid number of arguments.");
                }
                return new RestUpdateInvocation<TData, TId>(_invoker, (TId)arguments[0], (TData)arguments[1]);
            }
        }

        internal UpdateInvoker() { }

        public abstract ValueTask Invoke(HttpContext httpContext, object id, CancellationToken cancellationToken);
    }

    public sealed class UpdateInvoker<TData, TId> : UpdateInvoker
        where TData : class, IHasId<TId>
    {
        readonly IServiceProvider _serviceProvider;

        readonly RestAccessConfiguration _accessConfiguration;

        readonly IRestMethodInvoker _methodInvoker;

        readonly IRestUpdate<TData, TId> _implementation;

        readonly IDeserializer<TData> _deserializer;

        public UpdateInvoker(
            IServiceProvider serviceProvider,
            RestAccessConfiguration accessConfiguration,
            IRestMethodInvoker? methodInvoker = default,
            IRestUpdate<TData, TId>? implementation = default,
            IDeserializer<TData>? deserializer = default)
        {
            _serviceProvider = serviceProvider;
            _accessConfiguration = accessConfiguration;
            _methodInvoker = methodInvoker ?? DefaultRestMethodInvoker.Instance;
            _implementation = implementation ?? ActivatorUtilities.CreateInstance<DefaultRestUpdate<TData, TId>>(serviceProvider);
            _deserializer = deserializer ?? ActivatorUtilities.CreateInstance<DefaultDeserializer<TData>>(serviceProvider);
        }

        public override async ValueTask Invoke(HttpContext httpContext, object id, CancellationToken cancellationToken)
        {
            var accessValidator = _accessConfiguration.Update.CreateValidator(_serviceProvider, out var disposeValidator);
            try
            {
                if (!await accessValidator.ValidateAsync(httpContext.User, cancellationToken))
                {
                    throw new UnauthorizedException();
                }
                var data = await _deserializer.DeserializeAsync(httpContext.Request.Body, cancellationToken);
                var invocation = new RestUpdateInvocation<TData, TId>(_implementation, (TId)id, data);
                await _methodInvoker.InvokeAsync(invocation, cancellationToken);
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
}