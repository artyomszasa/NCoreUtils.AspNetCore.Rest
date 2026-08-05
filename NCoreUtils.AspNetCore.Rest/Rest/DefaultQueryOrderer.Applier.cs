using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NCoreUtils.AspNetCore.Rest;

public partial class DefaultQueryOrderer
{
    private abstract class Applier
    {
        public abstract IOrderedQueryable OrderBy(IQueryable source, LambdaExpression selector, bool isDescending);
        public abstract IOrderedQueryable ThenBy(IOrderedQueryable source, LambdaExpression selector, bool isDescending);
    }

    private sealed class Applier<TData, TKey> : Applier
    {
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Should be handled by the provider.")]
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

        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Should be handled by the provider.")]
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

    private static readonly ConcurrentDictionary<(Type dataType, Type keyType), Applier> _applierCache = new();

    private static readonly ConcurrentDictionary<(Type dataType, string memberName), PropertyInfo> _memberCache = new();

    private static readonly Func<(Type dataType, string memberName), PropertyInfo> _memberResolver = DoResolveMember;

    [UnconditionalSuppressMessage("Trimming", "IL2080", Justification = "Ensured by caller.")]
    private static PropertyInfo DoResolveMember((Type dataType, string memberName) args)
        => args.dataType.GetProperty(args.memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.IgnoreCase)
            ?? throw new InvalidOperationException($"Unable to resolve member {args.memberName} for type {args.dataType}.");


    private static bool TryGetSelectorType(LambdaExpression lambda, out (Type dataType, Type keyType) key)
    {
        if (1 == lambda.Parameters.Count)
        {
            key = (lambda.Parameters[0].Type, lambda.ReturnType);
            return true;
        }
        key = default;
        return false;
    }

    private static (Type dataType, Type keyType) GetSelectorType(Type expectedDataType, LambdaExpression lambda)
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
        return _applierCache.TryGetValue(key, out var applier)
            ? (IOrderedQueryable<TData>)applier.OrderBy(source, lambda, isDescending)
            : throw new InvalidOperationException($"No order by applier found for ({key.dataType.Name} => {key.keyType.Name}). Consider registering key selector using DefaultQueryOrderer.RegisterKeySelector or use custom order applier.");
    }

    protected static IOrderedQueryable<TData> ThenBy<TData>(IOrderedQueryable<TData> source, LambdaExpression lambda, bool isDescending)
    {
        if (lambda is null)
        {
            throw new ArgumentNullException(nameof(lambda));
        }
        var key = GetSelectorType(typeof(TData), lambda);
        return _applierCache.TryGetValue(key, out var applier)
            ? (IOrderedQueryable<TData>)applier.ThenBy(source, lambda, isDescending)
            : throw new InvalidOperationException($"No order by applier found for ({key.dataType.Name} => {key.keyType.Name}). Consider registering key selector using DefaultQueryOrderer.RegisterKeySelector or use custom order applier.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static IOrderedQueryable<TData> OrderBy<[DynamicallyAccessedMembers(AllProps)] TData>(IQueryable<TData> source, string memberName, bool isDescending)
        => OrderBy(source, CreateMemberSelector<TData>(memberName), isDescending);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static IOrderedQueryable<TData> ThenBy<[DynamicallyAccessedMembers(AllProps)] TData>(IOrderedQueryable<TData> source, string memberName, bool isDescending)
        => ThenBy(source, CreateMemberSelector<TData>(memberName), isDescending);

    protected static IQueryable<TData> OrderByDefaultProperty<[DynamicallyAccessedMembers(AllProps)] TData>(
        IQueryable<TData> source,
        IServiceProvider serviceProvider)
    {
        Preconditions.ThrowIfNull(serviceProvider);
        var property = serviceProvider.GetOptionalService<IDefaultOrderProperty<TData>>() switch
        {
            null => DefaultDefaultOrderProperty.GetDefaultOrderByProperty(typeof(TData)),
            var defaultOrderProperty => defaultOrderProperty.GetProperty()
        };
        return property.HasValue
            ? OrderBy(source, property.Property.CreateSelector(), property.IsDescending)
            : source;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RegisterKeySelector<TData, TKey>()
        => _applierCache.TryAdd((typeof(TData), typeof(TKey)), new Applier<TData, TKey>());

    public static void RegisterDefaultKeySelectors<TData>()
    {
        RegisterKeySelector<TData, bool>();
        RegisterKeySelector<TData, Guid>();
        RegisterKeySelector<TData, string>();
        RegisterKeySelector<TData, sbyte>();
        RegisterKeySelector<TData, short>();
        RegisterKeySelector<TData, int>();
        RegisterKeySelector<TData, long>();
#if NET7_0_OR_GREATER
        RegisterKeySelector<TData, UInt128>();
#endif
        RegisterKeySelector<TData, byte>();
        RegisterKeySelector<TData, ushort>();
        RegisterKeySelector<TData, uint>();
        RegisterKeySelector<TData, ulong>();
#if NET7_0_OR_GREATER
        RegisterKeySelector<TData, Int128>();
#endif
#if NET5_0_OR_GREATER
        RegisterKeySelector<TData, Half>();
#endif
        RegisterKeySelector<TData, float>();
        RegisterKeySelector<TData, double>();
        RegisterKeySelector<TData, DateOnly>();
        RegisterKeySelector<TData, TimeOnly>();
        RegisterKeySelector<TData, DateTime>();
        RegisterKeySelector<TData, DateTimeOffset>();
    }
}