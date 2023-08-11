using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NCoreUtils.AspNetCore.Rest.Serialization;

namespace NCoreUtils.AspNetCore.Rest.Internal
{
    public abstract class ListInvoker
    {
        internal static readonly AsyncQueryFilter _noFilter = (source, _) => new ValueTask<System.Linq.IQueryable>(source);

        private static async ValueTask<IReadOnlyList<TPartial>> ToList<TPartial>(IAsyncEnumerable<TPartial> enumerable, CancellationToken cancellationToken)
            => await enumerable.ToListAsync(cancellationToken);

        protected sealed class RestCollectionInvocation<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>
            : RestMethodEnumerableInvocation<T>
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

            public override IAsyncEnumerable<T> InvokeAsync(CancellationToken cancellationToken)
                => _invoker.InvokeAsync(_args.Arg1, _args.Arg2, cancellationToken);

            public override RestMethodEnumerableInvocation<T> UpdateArguments(IReadOnlyList<object> arguments)
            {
                if (arguments.Count != 2)
                {
                    throw new InvalidOperationException("Invalid number of arguments.");
                }
                return new RestCollectionInvocation<T>(_invoker, (RestQuery)arguments[0], (AsyncQueryFilter)arguments[1]);
            }
        }

        protected sealed class RestCollectionPartialInvocation<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TPartial>
            : RestMethodInvocation<IReadOnlyList<TPartial>>
        {
            readonly IRestListCollection<T> _invoker;

            readonly ArgumentCollection<RestQuery, AsyncQueryFilter, Expression<Func<T, TPartial>>> _args;

            public override Type ItemType => typeof(T);

            public override object Instance => _invoker;

            public override IReadOnlyList<object> Arguments => _args;

            public RestCollectionPartialInvocation(IRestListCollection<T> instance, RestQuery query, AsyncQueryFilter filter, Expression<Func<T, TPartial>> selector)
            {
                _invoker = instance;
                _args = new ArgumentCollection<RestQuery, AsyncQueryFilter, Expression<Func<T, TPartial>>>(query, filter, selector);
            }



            public override ValueTask<IReadOnlyList<TPartial>> InvokeAsync(CancellationToken cancellationToken = default)
            {
                var enumerable = _invoker.InvokePartialAsync(_args.Arg1, _args.Arg2, _args.Arg3, cancellationToken);
                return ToList<TPartial>(enumerable, cancellationToken);
            }

            public override RestMethodInvocation<IReadOnlyList<TPartial>> UpdateArguments(IReadOnlyList<object> arguments)
            {
                if (arguments.Count != 2)
                {
                    throw new InvalidOperationException("Invalid number of arguments.");
                }
                return new RestCollectionPartialInvocation<T, TPartial>(_invoker, (RestQuery)arguments[0], (AsyncQueryFilter)arguments[1], (Expression<Func<T, TPartial>>)arguments[2]);
            }
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ListInvoker<>))]
        internal ListInvoker() { }

        public abstract ValueTask Invoke(HttpContext httpContext, CancellationToken cancellationToken);
    }

    public sealed class ListInvoker<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T> : ListInvoker
    {
        private static readonly string Type = typeof(T).Name;

        private static readonly MethodInfo _gmDoInvokePartial;

        [UnconditionalSuppressMessage("Timming", "IL2075", Justification = "Preserved by dynamic dependency.")]
        static ListInvoker()
        {
            _gmDoInvokePartial = typeof(ListInvoker<T>)
                .GetMethod(nameof(DoInvokePartial), BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new InvalidOperationException($"Unable to get method {nameof(DoInvokePartial)} for {nameof(ListInvoker<T>)}.");
        }

        readonly IServiceProvider _serviceProvider;

        readonly RestAccessConfiguration _accessConfiguration;

        readonly IRestQueryParser _queryParser;

        readonly IRestMethodInvoker _methodInvoker;

        readonly IRestListCollection<T> _implementation;

        readonly ISerializerFactory _serializerFactory;

        readonly ILogger _logger;

        public ListInvoker(
            IServiceProvider serviceProvider,
            RestAccessConfiguration accessConfiguration,
            ILogger<ListInvoker> logger,
            IRestQueryParser? queryParser = null,
            IRestMethodInvoker? methodInvoker = null,
            IRestListCollection<T>? implementation = null,
            ISerializerFactory? serializerFactory = default)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _accessConfiguration = accessConfiguration ?? throw new ArgumentNullException(nameof(accessConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _queryParser = queryParser ?? new DefaultRestQueryParser();
            _methodInvoker = methodInvoker ?? DefaultRestMethodInvoker.Instance;
            _implementation = implementation ?? ActivatorUtilities.CreateInstance<DefaultRestListCollection<T>>(serviceProvider);
            _serializerFactory = serializerFactory ?? JsonTypeInfoSerializerFactory.Create(serviceProvider);
        }

        private sealed class ConfigurableBufferOutput : IConfigurableOutput<System.IO.Stream>
        {
            public long? ContentLength { get; private set; }

            public string? ContentType { get; private set; }

            public System.IO.MemoryStream? Buffer { get; private set; }

            public ValueTask<System.IO.Stream> InitializeAsync(OutputInfo info, CancellationToken cancellationToken)
            {
                ContentLength = info.Length;
                ContentType = info.ContentType;
                return new ValueTask<System.IO.Stream>(Buffer ??= new());
            }
        }

        private async ValueTask DoInvoke(
            Stopwatch stopwatch,
            RestQuery restQuery,
            AsyncQueryFilter filter,
            HttpResponse response,
            CancellationToken cancellationToken)
        {
            var invocation = new RestCollectionInvocation<T>(_implementation, restQuery, filter);
            var result = _methodInvoker.InvokeAsync(invocation, cancellationToken);
            var serializer = _serializerFactory.GetSerializer<IAsyncEnumerable<T>>();
            await serializer.SerializeAsync(new HttpResponseOutput(response), result, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task DoInvokeAndSerialize<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TPartial>(
            RestMethodInvocation<IReadOnlyList<TPartial>> invocation,
            HttpResponse response,
            CancellationToken cancellationToken)
        {
            var result = await _methodInvoker.InvokeAsync(invocation, cancellationToken);
            await _serializerFactory.GetSerializer<IReadOnlyList<TPartial>>()
                .SerializeAsync(new HttpResponseOutput(response), result, cancellationToken);
        }

        private Task DoInvokePartial<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TPartial>(
            RestQuery restQuery,
            AsyncQueryFilter filter,
            Expression<Func<T, TPartial>> selector,
            HttpResponse response,
            CancellationToken cancellationToken)
        {
            var invocation = new RestCollectionPartialInvocation<T, TPartial>(_implementation, restQuery, filter, selector);
            return DoInvokeAndSerialize(invocation, response, cancellationToken);
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Dynamically emitted members cannot be trimmed.")]
        [UnconditionalSuppressMessage("Trimming", "IL2060", Justification = "Dynamically emitted members cannot be trimmed.")]
        public override async ValueTask Invoke(HttpContext context, CancellationToken cancellationToken)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var accessValidator = _accessConfiguration.Query.GetOrCreateValidator(_serviceProvider, out var disposeValidator);
            try
            {
                var validationResult = await accessValidator.ValidateAsync(context.User, cancellationToken);
                _logger.LogTrace("[{Type}] Access validation ({AccessAllowed}) done ({Elapsed}ms).", Type, validationResult.Success, stopwatch.ElapsedMilliseconds);
                validationResult.ThrowOnFailure();
                var filter = null != accessValidator && accessValidator is IQueryAccessStatusValidator queryAccessValidator
                    ? new AsyncQueryFilter((source, ctoken) => queryAccessValidator.FilterQueryAsync(source, context.User, ctoken))
                    : ListInvoker._noFilter;
                using var restQuery = await _queryParser.ParseAsync(context.Request, cancellationToken);
                _logger.LogTrace("[{Type}] REST query parsing done ({Elapsed}ms).", Type, stopwatch.ElapsedMilliseconds);
                if (!restQuery.Fields.HasValue || restQuery.Fields.Value.Count == 0)
                {
                    await DoInvoke(stopwatch, restQuery, filter, context.Response, cancellationToken);
                }
                else
                {
                    var classInfo = PartialClassFactory.CreatePartialClass(typeof(T), restQuery.Fields);
                    await (Task)_gmDoInvokePartial
                        .MakeGenericMethod(classInfo.Type)
                        .Invoke(this, new object[] { restQuery, filter, classInfo.Selector, context.Response, cancellationToken })!;
                }
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