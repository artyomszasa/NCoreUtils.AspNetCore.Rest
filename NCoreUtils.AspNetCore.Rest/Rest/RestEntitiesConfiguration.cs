using NCoreUtils.AspNetCore.Rest.Internal;

#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#else
using System.Collections.Immutable;
#endif

namespace NCoreUtils.AspNetCore.Rest;

#if NET8_0_OR_GREATER

public class RestEntitiesConfiguration
{
    private FrozenDictionary<Type, string> EntityNames { get; }

    private FrozenDictionary<CaseInsensitive, Type> EntityTypes { get; }

    public FrozenDictionary<Type, EndpointInvoker> Invokers { get; }

    internal RestEntitiesConfiguration(
        FrozenDictionary<Type, string> entityNames,
        FrozenDictionary<CaseInsensitive, Type> entityTypes,
        FrozenDictionary<Type, EndpointInvoker> invokers)
    {
        EntityNames = entityNames ?? throw new ArgumentNullException(nameof(entityNames));
        EntityTypes = entityTypes ?? throw new ArgumentNullException(nameof(entityTypes));
        Invokers = invokers ?? throw new ArgumentNullException(nameof(invokers));
    }

    public RestEntitiesConfiguration(IEnumerable<KeyValuePair<Type, string>> entries, IEnumerable<KeyValuePair<Type, EndpointInvoker>> invokers)
        : this(
            entries.ToFrozenDictionary(),
            entries.ToFrozenDictionary(e => CaseInsensitive.Create(e.Value), e => e.Key),
            invokers.ToFrozenDictionary())
    { }


    public bool TryResolveType(CaseInsensitive name, [MaybeNullWhen(false)] out Type type)
        => EntityTypes.TryGetValue(name, out type);

    public bool TryGetName(Type type, [MaybeNullWhen(false)] out string name)
        => EntityNames.TryGetValue(type, out name);
}

#else

public class RestEntitiesConfiguration
{
    private ImmutableDictionary<Type, string> EntityNames { get; }

    private ImmutableDictionary<CaseInsensitive, Type> EntityTypes { get; }

    public ImmutableDictionary<Type, EndpointInvoker> Invokers { get; }

    internal RestEntitiesConfiguration(
        ImmutableDictionary<Type, string> entityNames,
        ImmutableDictionary<CaseInsensitive, Type> entityTypes,
        ImmutableDictionary<Type, EndpointInvoker> invokers)
    {
        EntityNames = entityNames ?? throw new ArgumentNullException(nameof(entityNames));
        EntityTypes = entityTypes ?? throw new ArgumentNullException(nameof(entityTypes));
        Invokers = invokers ?? throw new ArgumentNullException(nameof(invokers));
    }

    public RestEntitiesConfiguration(IEnumerable<KeyValuePair<Type, string>> entries, IEnumerable<KeyValuePair<Type, EndpointInvoker>> invokers)
        : this(
            entries.ToImmutableDictionary(),
            entries.ToImmutableDictionary(e => CaseInsensitive.Create(e.Value), e => e.Key),
            invokers.ToImmutableDictionary())
    { }


    public bool TryResolveType(CaseInsensitive name, [MaybeNullWhen(false)] out Type type)
        => EntityTypes.TryGetValue(name, out type);

    public bool TryGetName(Type type, [MaybeNullWhen(false)] out string name)
        => EntityNames.TryGetValue(type, out name);
}

#endif