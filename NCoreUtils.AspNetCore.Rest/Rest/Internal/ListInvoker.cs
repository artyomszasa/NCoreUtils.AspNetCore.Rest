using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace NCoreUtils.AspNetCore.Rest.Internal
{
    public abstract class ListInvoker
    {
        internal static readonly AsyncQueryFilter _noFilter = (source, _) => new ValueTask<System.Linq.IQueryable>(source);

        protected sealed class RestCollectionInvocation<T> : RestMethodInvocation<IReadOnlyList<T>>
        {
            readonly IRestListCollection<T> _invoker;

            readonly ArgumentCollection<RestQuery, AsyncQueryFilter> _args;

            public override Type ItemType => typeof(T);

            public override object Instance => _invoker;

            public override IReadOnlyList<object> Arguments => _args;

            public RestCollectionInvocation(IRestListCollection<T> instance, RestQuery query, AsyncQueryFilter filter)
            {
                _invoker = instance;
                _args = new ArgumentCollection<RestQuery, AsyncQueryFilter>(query, filter);
            }

            public override async ValueTask<IReadOnlyList<T>> InvokeAsync(CancellationToken cancellationToken = default)
                => await _invoker.InvokeAsync(_args.Arg1, _args.Arg2, cancellationToken).ToListAsync(cancellationToken);

            public override RestMethodInvocation<IReadOnlyList<T>> UpdateArguments(IReadOnlyList<object> arguments)
            {
                if (arguments.Count != 2)
                {
                    throw new InvalidOperationException("Invalid number of arguments.");
                }
                return new RestCollectionInvocation<T>(_invoker, (RestQuery)arguments[0], (AsyncQueryFilter)arguments[1]);
            }
        }

        internal ListInvoker() { }

        public abstract ValueTask Invoke(HttpContext httpContext, CancellationToken cancellationToken);
    }

    public sealed class ListInvoker<T> : ListInvoker
    {
        readonly IServiceProvider _serviceProvider;

        readonly RestAccessConfiguration _accessConfiguration;

        readonly IRestQueryParser _queryParser;

        readonly IRestMethodInvoker _methodInvoker;

        readonly IRestListCollection<T> _implementation;

        ISerializer<IReadOnlyList<T>> _serializer;

        public ListInvoker(
            IServiceProvider serviceProvider,
            RestAccessConfiguration accessConfiguration,
            IRestQueryParser? queryParser = null,
            IRestMethodInvoker? methodInvoker = null,
            IRestListCollection<T>? implementation = null,
            ISerializer<IReadOnlyList<T>>? serializer = null)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _accessConfiguration = accessConfiguration ?? throw new ArgumentNullException(nameof(accessConfiguration));
            _queryParser = queryParser ?? new DefaultRestQueryParser();
            _methodInvoker = methodInvoker ?? DefaultRestMethodInvoker.Instance;
            _implementation = implementation ?? ActivatorUtilities.CreateInstance<DefaultRestListCollection<T>>(serviceProvider);
            _serializer = serializer ?? ActivatorUtilities.CreateInstance<DefaultSerializer<IReadOnlyList<T>>>(serviceProvider);
        }

        public override async ValueTask Invoke(HttpContext context, CancellationToken cancellationToken)
        {
            var accessValidator = _accessConfiguration.Query.CreateValidator(_serviceProvider, out var disposeValidator);
            try
            {
                if (!await accessValidator.ValidateAsync(context.User, cancellationToken))
                {
                    throw new UnauthorizedException();
                }
                var filter = null != accessValidator && accessValidator is IQueryAccessValidator queryAccessValidator
                    ? (source, ctoken) => queryAccessValidator.FilterQueryAsync(source, context.User, ctoken)
                    : ListInvoker._noFilter;
                var restQuery = await _queryParser.ParseAsync(context.Request, cancellationToken);
                var invocation = new RestCollectionInvocation<T>(_implementation, restQuery, filter);
                var result = await _methodInvoker.InvokeAsync(invocation, cancellationToken);
                await _serializer.SerializeAsync(new HttpResponseOutput(context.Response), result, cancellationToken);
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