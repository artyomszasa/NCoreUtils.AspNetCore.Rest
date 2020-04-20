using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace NCoreUtils.AspNetCore.Rest.Internal
{
    public abstract class ReductionInvoker
    {
        protected sealed class RestReductionInvocation<T> : RestMethodInvocation<object?>
        {
            private readonly IRestReduction<T> _invoker;

            private readonly ArgumentCollection<RestQuery, string, AsyncQueryFilter> _args;

            public override Type ItemType => typeof(T);

            public override object Instance => _invoker;

            public override IReadOnlyList<object> Arguments => _args;

            public RestReductionInvocation(IRestReduction<T> invoker, RestQuery restQuery, string reduction, AsyncQueryFilter filter)
            {
                _invoker = invoker;
                _args = new ArgumentCollection<RestQuery, string, AsyncQueryFilter>(restQuery, reduction, filter);
            }

            public override ValueTask<object?> InvokeAsync(CancellationToken cancellationToken = default)
                => _invoker.InvokeAsync(_args.Arg1, _args.Arg2, _args.Arg3, cancellationToken);

            public override RestMethodInvocation<object?> UpdateArguments(IReadOnlyList<object> arguments)
            {
                if (arguments.Count != 3)
                {
                    throw new InvalidOperationException("Invalid number of arguments.");
                }
                return new RestReductionInvocation<T>(_invoker, (RestQuery)arguments[0], (string)arguments[1], (AsyncQueryFilter)arguments[2]);
            }
        }

        internal ReductionInvoker() { }

        public abstract ValueTask Invoke(HttpContext httpContext, string reduction, CancellationToken cancellationToken);
    }

    public sealed class ReductionInvoker<T> : ReductionInvoker
        where T : class
    {
        readonly IServiceProvider _serviceProvider;

        readonly RestAccessConfiguration _accessConfiguration;

        readonly IRestQueryParser _queryParser;

        readonly IRestMethodInvoker _methodInvoker;

        readonly IRestReduction<T> _implementation;

        readonly ISerializerFactory _serializerFactory;

        public ReductionInvoker(
            IServiceProvider serviceProvider,
            RestAccessConfiguration accessConfiguration,
            IRestQueryParser? queryParser = default,
            IRestMethodInvoker? methodInvoker = default,
            IRestReduction<T>? implementation = default,
            ISerializerFactory? serializerFactory = default)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _accessConfiguration = accessConfiguration ?? throw new ArgumentNullException(nameof(accessConfiguration));
            _queryParser = queryParser ?? new DefaultRestQueryParser();
            _methodInvoker = methodInvoker ?? DefaultRestMethodInvoker.Instance;
            _implementation = implementation ?? ActivatorUtilities.CreateInstance<DefaultRestReduction<T>>(serviceProvider);
            _serializerFactory = serializerFactory ?? ActivatorUtilities.CreateInstance<DefaultSerializerFactory>(serviceProvider);
        }

        public override async ValueTask Invoke(HttpContext httpContext, string reduction, CancellationToken cancellationToken)
        {
            var accessValidator = _accessConfiguration.Query.CreateValidator(_serviceProvider, out var disposeValidator);
            try
            {
                if (!await accessValidator.ValidateAsync(httpContext.User, cancellationToken))
                {
                    throw new UnauthorizedException();
                }
                var filter = null != accessValidator && accessValidator is IQueryAccessValidator queryAccessValidator
                    ? (source, ctoken) => queryAccessValidator.FilterQueryAsync(source, httpContext.User, ctoken)
                    : ListInvoker._noFilter;
                var restQuery = await _queryParser.ParseAsync(httpContext.Request, cancellationToken);
                var invocation = new RestReductionInvocation<T>(_implementation, restQuery, reduction, filter);
                var result = await _methodInvoker.InvokeAsync(invocation, cancellationToken);
                if (result is null)
                {
                    httpContext.Response.StatusCode = 204;
                    return;
                }
                await _serializerFactory.SerializeAsync(
                    new HttpResponseOutput(httpContext.Response),
                    result,
                    cancellationToken);
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