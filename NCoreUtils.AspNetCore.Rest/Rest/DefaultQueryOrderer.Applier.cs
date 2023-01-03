using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NCoreUtils.AspNetCore.Rest
{
    public partial class DefaultQueryOrderer
    {
        abstract class Applier
        {
            public abstract IOrderedQueryable OrderBy(IQueryable source, LambdaExpression selector, bool isDescending);
            public abstract IOrderedQueryable ThenBy(IOrderedQueryable source, LambdaExpression selector, bool isDescending);
        }

        sealed class Applier<TData, TKey> : Applier
        {
            public override IOrderedQueryable OrderBy(IQueryable source, LambdaExpression lambda, bool isDescending)
                => source switch
                {
                    null => throw new ArgumentNullException(nameof(source)),
                    IQueryable<TData> queryable => lambda switch
                    {
                        null => throw new ArgumentNullException(nameof(lambda)),
                        Expression<Func<TData, TKey>> selector => isDescending
                            ? queryable.OrderByDescending(selector)
                            : queryable.OrderBy(selector),
                        _ => throw new ArgumentException("Invalid selector.", nameof(lambda))
                    },
                    _ => throw new ArgumentException("Invalid source.", nameof(source))
                };

            public override IOrderedQueryable ThenBy(IOrderedQueryable source, LambdaExpression lambda, bool isDescending)
                => source switch
                {
                    null => throw new ArgumentNullException(nameof(source)),
                    IOrderedQueryable<TData> queryable => lambda switch
                    {
                        null => throw new ArgumentNullException(nameof(lambda)),
                        Expression<Func<TData, TKey>> selector => isDescending
                            ? queryable.ThenByDescending(selector)
                            : queryable.ThenBy(selector),
                        _ => throw new ArgumentException("Invalid selector.", nameof(lambda))
                    },
                    _ => throw new ArgumentException("Invalid source.", nameof(source))
                };
        }

        private const DynamicallyAccessedMemberTypes AllProps = DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties;

        static readonly ConcurrentDictionary<(Type dataType, Type keyType), Applier> _applierCache = new ConcurrentDictionary<(Type dataType, Type keyType), Applier>();

        static readonly ConcurrentDictionary<(Type dataType, string memberName), PropertyInfo> _memberCache = new ConcurrentDictionary<(Type dataType, string memberName), PropertyInfo>();

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Only known types are passed.")]
        private static readonly Func<(Type dataType, Type keyType), Applier> _applierFactory =
            key => (Applier)Activator.CreateInstance(typeof(Applier<,>).MakeGenericType(key.dataType, key.keyType), true)!;


        private static readonly Func<(Type dataType, string memberName), PropertyInfo> _memberResolver = DoResolveMember;

        [UnconditionalSuppressMessage("Trimming", "IL2080", Justification = "Ensured by caller.")]
        private static PropertyInfo DoResolveMember((Type dataType, string memberName) args)
        {
            var property = args.dataType.GetProperty(args.memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.IgnoreCase);
            if (property is null)
            {
                throw new InvalidOperationException($"Unable to resolve member {args.memberName} for type {args.dataType}.");
            }
            return property;
        }


        static bool TryGetSelectorType(LambdaExpression lambda, out (Type dataType, Type keyType) key)
        {
            if (1 == lambda.Parameters.Count)
            {
                key = (lambda.Parameters[0].Type, lambda.ReturnType);
                return true;
            }
            key = default;
            return false;
        }

        static (Type dataType, Type keyType) GetSelectorType(Type expectedDataType, LambdaExpression lambda)
        {
            if (TryGetSelectorType(lambda, out var key) && key.dataType == expectedDataType)
            {
                return key;
            }
            throw new InvalidOperationException($"Invalid selector: ${lambda}.");
        }

        protected static LambdaExpression CreateMemberSelector<[DynamicallyAccessedMembers(AllProps)] TData>(string memberName)
            => _memberCache.GetOrAdd((typeof(TData), memberName), _memberResolver).CreateSelector();

        protected static IOrderedQueryable<TData> OrderBy<TData>(IQueryable<TData> source, LambdaExpression lambda, bool isDescending)
        {
            if (lambda is null)
            {
                throw new ArgumentNullException(nameof(lambda));
            }
            var key = GetSelectorType(typeof(TData), lambda);
            return (IOrderedQueryable<TData>)_applierCache.GetOrAdd(key, _applierFactory).OrderBy(source, lambda, isDescending);
        }

        protected static IOrderedQueryable<TData> ThenBy<TData>(IOrderedQueryable<TData> source, LambdaExpression lambda, bool isDescending)
        {
            if (lambda is null)
            {
                throw new ArgumentNullException(nameof(lambda));
            }
            var key = GetSelectorType(typeof(TData), lambda);
            return (IOrderedQueryable<TData>)_applierCache.GetOrAdd(key, _applierFactory).ThenBy(source, lambda, isDescending);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static IOrderedQueryable<TData> OrderBy<[DynamicallyAccessedMembers(AllProps)] TData>(IQueryable<TData> source, string memberName, bool isDescending)
            => OrderBy(source, CreateMemberSelector<TData>(memberName), isDescending);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static IOrderedQueryable<TData> ThenBy<[DynamicallyAccessedMembers(AllProps)] TData>(IOrderedQueryable<TData> source, string memberName, bool isDescending)
            => ThenBy(source, CreateMemberSelector<TData>(memberName), isDescending);

        protected static IOrderedQueryable<TData> OrderByDefaultProperty<[DynamicallyAccessedMembers(AllProps)] TData>(
            IQueryable<TData> source,
            IServiceProvider serviceProvider)
        {
            var defaultOrderProperty = serviceProvider.GetOptionalService<IDefaultOrderProperty<TData>>();
            var property = defaultOrderProperty is null
                ? DefaultDefaultOrderProperty.GetDefaultOrderByProperty(typeof(TData))
                : defaultOrderProperty.Select();
            return OrderBy(source, property.Property.CreateSelector(), property.IsDescending);
        }
    }
}