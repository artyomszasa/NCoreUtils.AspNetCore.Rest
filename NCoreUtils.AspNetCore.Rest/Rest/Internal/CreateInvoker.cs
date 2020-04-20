using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NCoreUtils.Data;

namespace NCoreUtils.AspNetCore.Rest.Internal
{
    public abstract class CreateInvoker
    {
        protected sealed class RestCreateInvocation<TData, TId> : RestMethodInvocation<TData>
            where TData : notnull, IHasId<TId>
        {
            readonly IRestCreate<TData, TId> _invoker;

            readonly ArgumentCollection<TData> _args;

            public override Type ItemType => typeof(TData);

            public override object Instance => _invoker;

            public override IReadOnlyList<object> Arguments => _args;

            public RestCreateInvocation(IRestCreate<TData, TId> invoker, TData data)
            {
                _invoker = invoker;
                _args = new ArgumentCollection<TData>(data);
            }

            public override ValueTask<TData> InvokeAsync(CancellationToken cancellationToken = default)
                => _invoker.InvokeAsync(_args.Arg, cancellationToken);

            public override RestMethodInvocation<TData> UpdateArguments(IReadOnlyList<object> arguments)
            {
                if (arguments.Count != 1)
                {
                    throw new InvalidOperationException("Invalid number of arguments.");
                }
                return new RestCreateInvocation<TData, TId>(_invoker, (TData)arguments[0]);
            }
        }

        internal CreateInvoker() { }

        public abstract ValueTask Invoke(HttpContext httpContext, CancellationToken cancellationToken);
    }

    public sealed class CreateInvoker<TData, TId> : CreateInvoker
        where TData : notnull, IHasId<TId>
    {
        readonly IServiceProvider _serviceProvider;

        readonly RestAccessConfiguration _accessConfiguration;

        readonly IRestMethodInvoker _methodInvoker;

        readonly IRestCreate<TData, TId> _implementation;

        readonly IDeserializer<TData> _deserializer;

        public CreateInvoker(
            IServiceProvider serviceProvider,
            RestAccessConfiguration accessConfiguration,
            IRestMethodInvoker methodInvoker,
            IRestCreate<TData, TId> implementation,
            IDeserializer<TData> deserializer)
        {
            _serviceProvider = serviceProvider;
            _accessConfiguration = accessConfiguration;
            _methodInvoker = methodInvoker;
            _implementation = implementation;
            _deserializer = deserializer;
        }


        public override async ValueTask Invoke(HttpContext httpContext, CancellationToken cancellationToken)
        {
            var accessValidator = _accessConfiguration.Create.CreateValidator(_serviceProvider, out var disposeValidator);
            try
            {
                if (!await accessValidator.ValidateAsync(httpContext.User, cancellationToken))
                {
                    throw new UnauthorizedException();
                }
                var data = await _deserializer.DeserializeAsync(httpContext.Request.Body, cancellationToken);
                var invocation = new RestCreateInvocation<TData, TId>(_implementation, data);
                await _methodInvoker.InvokeAsync(invocation, cancellationToken);
                // FIXME: add location
                // httpContext.Response.Headers.Append("Location", );
                httpContext.Response.StatusCode = 201;
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