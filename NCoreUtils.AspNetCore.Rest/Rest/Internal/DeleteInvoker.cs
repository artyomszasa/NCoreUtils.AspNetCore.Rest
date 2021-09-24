using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.Data;

namespace NCoreUtils.AspNetCore.Rest.Internal
{
    public abstract class DeleteInvoker
    {
        protected sealed class RestDeleteInvocation<TData, TId> : ViodRestMethodInvocation
            where TData : IHasId<TId>
        {
            private readonly IRestDelete<TData, TId> _invoker;

            private readonly ArgumentCollection<TId, bool> _args;

            public override Type ItemType => typeof(TData);

            public override object Instance => _invoker;

            public override IReadOnlyList<object> Arguments => _args;

            public RestDeleteInvocation(IRestDelete<TData, TId> invoker, TId id, bool force)
            {
                _invoker = invoker;
                _args = new ArgumentCollection<TId, bool>(id, force);
            }

            public override ValueTask InvokeAsync(CancellationToken cancellationToken = default)
                => _invoker.InvokeAsync(_args.Arg1, _args.Arg2, cancellationToken);

            public override ViodRestMethodInvocation UpdateArguments(IReadOnlyList<object> arguments)
            {
                if (arguments.Count != 2)
                {
                    throw new InvalidOperationException("Invalid number of arguments.");
                }
                return new RestDeleteInvocation<TData, TId>(_invoker, (TId)arguments[0], (bool)arguments[1]);
            }
        }

        internal DeleteInvoker() { }

        public abstract ValueTask Invoke(HttpContext httpContext, object id, bool force, CancellationToken cancellationToken);
    }

    public sealed class DeleteInvoker<TData, TId> : DeleteInvoker
        where TData : IHasId<TId>
    {
        readonly IServiceProvider _serviceProvider;

        readonly RestAccessConfiguration _accessConfiguration;

        readonly IRestMethodInvoker _methodInvoker;

        readonly IRestDelete<TData, TId> _implementation;

        public DeleteInvoker(
            IServiceProvider serviceProvider,
            RestAccessConfiguration accessConfiguration,
            IRestMethodInvoker? methodInvoker = default,
            IRestDelete<TData, TId>? implementation = default)
        {
            _serviceProvider = serviceProvider;
            _accessConfiguration = accessConfiguration;
            _methodInvoker = methodInvoker ?? DefaultRestMethodInvoker.Instance;
            _implementation = implementation ?? ActivatorUtilities.CreateInstance<DefaultRestDelete<TData, TId>>(serviceProvider);
        }

        public override async ValueTask Invoke(HttpContext httpContext, object id, bool force, CancellationToken cancellationToken)
        {
            var accessValidator = _accessConfiguration.Delete.CreateValidator(_serviceProvider, out var disposeValidator);
            try
            {
                if (!await accessValidator.ValidateAsync(httpContext.User, cancellationToken))
                {
                    throw new UnauthorizedException();
                }
                var invocation = new RestDeleteInvocation<TData, TId>(_implementation, (TId)id, force);
                await _methodInvoker.InvokeAsync(invocation, cancellationToken);
                httpContext.Response.StatusCode = 200;
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