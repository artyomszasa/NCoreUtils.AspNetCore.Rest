using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.AspNetCore.Rest;

internal static class QueryableExtensions
{
    sealed class ValueBox<T>(T value)
    {
        public T Value { get; } = value;
    }

    internal static IQueryable<T> Apply<T>(this IQueryable<T> source, IRestQueryFilter<T> filter, RestQuery restQuery)
        => filter.ApplyFilters(source, restQuery);

    internal static IQueryable<T> Apply<T>(this IQueryable<T> source, IRestQueryOrderer<T> orderer, RestQuery restQuery)
        => orderer.ApplyOrder(source, restQuery);

    internal static async ValueTask<IQueryable<T>> ApplyAsync<T>(this IQueryable<T> source, AsyncQueryFilter filter, CancellationToken cancellationToken)
        => (IQueryable<T>)await filter(source, cancellationToken);

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ValueBox<>))]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Only known types are passed.")]
    public static Expression BoxConstant<T>(T value)
    {
        var box = new ValueBox<T>(value);
        return Expression.Property(Expression.Constant(box), nameof(ValueBox<T>.Value));
    }

#if NET7_0
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Should handled by query provider.")]
#endif
    public static IQueryable<T> TakeWhenNonNegative<T>(this IQueryable<T> source, int value)
        => value < 0 ? source : source.Take(value);

#if NET7_0
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Should handled by query provider.")]
#endif
    public static IQueryable<T> TakeWhenNonNegative<T>(this IQueryable<T> source, int? value)
        => value is not int v || v < 0 ? source : source.Take(v);
}