using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.AspNetCore.Rest.Serialization;

namespace NCoreUtils.AspNetCore.Rest.Internal;

public abstract class ReductionInvoker
{
    protected sealed class RestReductionInvocation<T>(IRestReduction<T> invoker, IRestReductionContext<T> context)
        : RestMethodInvocation<object?>
    {
        private readonly ArgumentCollection<IRestReductionContext<T>> Args = new(context);

        private IRestReduction<T> Invoker { get; } = invoker;

        public override Type ItemType => typeof(T);

        public override object Instance => Invoker;

        public override IReadOnlyList<object> Arguments => Args;

        public override ValueTask<object?> InvokeAsync(CancellationToken cancellationToken = default)
            => Invoker.InvokeAsync(Args.Arg, cancellationToken);

        public override RestMethodInvocation<object?> UpdateArguments(IReadOnlyList<object> arguments)
        {
            if (arguments.ThrowIfNull().Count != 1)
            {
                throw new InvalidOperationException("Invalid number of arguments.");
            }
            return new RestReductionInvocation<T>(Invoker, (IRestReductionContext<T>)arguments[0]);
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2068", Justification = "Used internally to suppress unrelevant warning.")]
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
    protected static Type SuppressWarnings(Type type)
        => type;

    internal ReductionInvoker() { }

    public abstract ValueTask Invoke(HttpContext httpContext, IRestContextMetadata metadata, string reduction, CancellationToken cancellationToken);
}

public sealed class ReductionInvoker<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TData, TId>(
    IServiceProvider serviceProvider,
    RestAccessConfiguration accessConfiguration,
    IRestQueryParser? queryParser = default,
    IRestMethodInvoker? methodInvoker = default,
    IRestReduction<TData>? implementation = default,
    ISerializerFactory? serializerFactory = default) : ReductionInvoker
    where TData : class
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    private readonly RestAccessConfiguration _accessConfiguration = accessConfiguration ?? throw new ArgumentNullException(nameof(accessConfiguration));

    private readonly IRestQueryParser _queryParser = queryParser ?? new DefaultRestQueryParser();

    private readonly IRestMethodInvoker _methodInvoker = methodInvoker ?? DefaultRestMethodInvoker.Instance;

    private readonly IRestReduction<TData> _implementation = implementation ?? ActivatorUtilities.CreateInstance<DefaultRestReduction<TData>>(serviceProvider);

    private readonly ISerializerFactory _serializerFactory = serializerFactory ?? JsonTypeInfoSerializerFactory.Create(serviceProvider);

    public override async ValueTask Invoke(HttpContext httpContext, IRestContextMetadata metadata, string reduction, CancellationToken cancellationToken)
    {
        var accessValidator = _accessConfiguration.Query.GetOrCreateValidator(_serviceProvider, out var disposeValidator);
        try
        {
            (await accessValidator.ValidateAsync(httpContext.User, cancellationToken).ConfigureAwait(false)).ThrowOnFailure();
            var filter = null != accessValidator && accessValidator is IQueryAccessStatusValidator queryAccessValidator
                ? new AsyncQueryFilter((source, ctoken) => queryAccessValidator.FilterQueryAsync(source, httpContext.User, ctoken))
                : ListInvoker._noFilter;
            using var restQuery = await _queryParser.ParseAsync(httpContext.Request, cancellationToken);
            var context = RestContext.Reduction<TData, TId>(restQuery, reduction, filter, metadata);
            var invocation = new RestReductionInvocation<TData>(_implementation, context);
            object? result;
            // using (var activity = G.ActivitySource.StartActivity("REST REDUCTION method execution"))
            {
                result = await _methodInvoker.InvokeAsync(invocation, cancellationToken).ConfigureAwait(false);
            }
            if (result is null)
            {
                httpContext.Response.StatusCode = 204;
                return;
            }
            // using var activity = G.ActivitySource.StartActivity("REST REDUCTION method result serialization");
            await _serializerFactory
                .SerializeAsync(
                    new HttpResponseOutput(httpContext.Response),
                    result,
                    SuppressWarnings(result.GetType()),
                    cancellationToken
                )
                .ConfigureAwait(false);
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