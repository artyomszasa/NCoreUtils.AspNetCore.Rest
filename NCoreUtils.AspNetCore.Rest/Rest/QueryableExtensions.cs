using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Data;

namespace NCoreUtils.AspNetCore.Rest
{
    static class QueryableExtensions
    {
        sealed class ValueBox<T>
        {
            public T Value { get; }

            public ValueBox(T value) => Value = value;
        }

        static readonly MethodInfo _gmCreateIdSelector = typeof(QueryableExtensions)
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .First(m => m.Name == nameof(CreateIdSelector) && m.IsGenericMethodDefinition);

        static readonly ConcurrentDictionary<Type, LambdaExpression> _idSelectorCache = new ConcurrentDictionary<Type, LambdaExpression>();

        static readonly Func<Type, LambdaExpression> _idSelectorFactory = type =>
        {
            if (NCoreUtils.Data.IdUtils.TryGetIdType(type, out var idType))
            {
                return (LambdaExpression)_gmCreateIdSelector.MakeGenericMethod(type, idType).Invoke(null, new object[0])!;
            }
            throw new InvalidOperationException($"Invalid entity type {type}.");
        };

        static Expression BoxConstant<T>(T value)
        {
            var box = new ValueBox<T>(value);
            return Expression.Property(Expression.Constant(box), nameof(ValueBox<T>.Value));
        }

        static Expression<Func<TData, TId>> CreateIdSelector<TData, TId>()
            where TData : IHasId<TId>
            => LinqExtensions.ReplaceExplicitProperties<Func<TData, TId>>(e => e.Id);

        static LambdaExpression GetOrCreateIdSelector(Type type)
            => _idSelectorCache.GetOrAdd(type, _idSelectorFactory);

        internal static IQueryable<T> Apply<T>(this IQueryable<T> source, IRestQueryFilter<T> filter, RestQuery restQuery)
            => filter.ApplyFilters(source, restQuery);

        internal static IOrderedQueryable<T> Apply<T>(this IQueryable<T> source, IRestQueryOrderer<T> orderer, RestQuery restQuery)
            => orderer.ApplyOrder(source, restQuery);

        internal static async ValueTask<IQueryable<T>> ApplyAsync<T>(this IQueryable<T> source, AsyncQueryFilter filter, CancellationToken cancellationToken)
            => (IQueryable<T>)await filter(source, cancellationToken);

        public static Expression<Func<TData, bool>> CreateIdEqualityPredicate<TData, TId>(TId value)
            where TData : IHasId<TId>
        {
            var idSelector = GetOrCreateIdSelector(typeof(TData));
            var eArg = Expression.Parameter(typeof(TData));
            return Expression.Lambda<Func<TData, bool>>(
                Expression.Equal(
                    BoxConstant(value),
                    idSelector.SubstituteParameter(idSelector.Parameters[0], eArg)
                ),
                eArg
            );
        }

        public static IQueryable<T> TakeWhenNonNegative<T>(this IQueryable<T> source, int value)
            => value < 0 ? source : source.Take(value);
    }
}