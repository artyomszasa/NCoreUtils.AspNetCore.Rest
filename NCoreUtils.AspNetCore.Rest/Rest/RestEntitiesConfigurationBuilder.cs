using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NCoreUtils.AspNetCore.Rest.Internal;
using NCoreUtils.AspNetCore.Rest.Serialization;
using NCoreUtils.Data;

#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#else
using System.Collections.Immutable;
#endif

namespace NCoreUtils.AspNetCore.Rest;

public class RestEntitiesConfigurationBuilder
{
    private const string WarnAll = "When using this method JsonTypeInfoSerializerFactory.RegisterSerializableType must be called manually and EndpointInvoker must be added manually!";

    public const string WarnEndpointInvoker = "When using this method EndpointInvoker must be added manually!";

    private Dictionary<Type, string> EntityNames { get; } = [];

    private Dictionary<CaseInsensitive, Type> EntityTypes { get; } = [];

    public Dictionary<Type, EndpointInvoker> Invokers { get; } = [];

    private RestEntitiesConfigurationBuilder AddInternal(Type type, CaseInsensitive name)
    {
        if (EntityNames.ContainsKey(type))
        {
            throw new InvalidOperationException($"{type} has already been registered.");
        }
        if (EntityTypes.TryGetValue(name, out var xtype))
        {
            throw new InvalidOperationException($"{xtype} has already been registered with name = {name}.");
        }
        EntityNames.Add(type, name.ToLowerString());
        EntityTypes.Add(name, type);
        return this;
    }

    [Obsolete(WarnAll)]
    public RestEntitiesConfigurationBuilder Add(Type type, CaseInsensitive name)
        => AddInternal(type, name);

    [Obsolete(WarnAll)]
    public RestEntitiesConfigurationBuilder Add(Type type)
        => AddInternal(type, type.Name.ToLowerInvariant());

    [Obsolete(WarnEndpointInvoker)]
    public RestEntitiesConfigurationBuilder Add<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>(CaseInsensitive name)
    {
        JsonTypeInfoSerializerFactory.RegisterSerializableType<T>();
        JsonTypeInfoSerializerFactory.RegisterSerializableType<IAsyncEnumerable<T>>();
        DefaultQueryOrderer.RegisterDefaultKeySelectors<T>();
        return AddInternal(typeof(T), name);
    }

    [Obsolete(WarnEndpointInvoker)]
    public RestEntitiesConfigurationBuilder Add<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>()
        => Add<T>(typeof(T).Name.ToLowerInvariant());

    [Obsolete(WarnAll)]
    public RestEntitiesConfigurationBuilder AddRange(params Type[] types)
    {
        foreach (var type in types)
        {
            Add(type);
        }
        return this;
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(EndpointInvokerInt32<>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(EndpointInvokerString<>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(EndpointInvokerGuid<>))]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Everything preserved manually.")]
    public RestEntitiesConfigurationBuilder Add<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TData, TId>(CaseInsensitive name)
        where TData : class, IHasId<TId>
    {
        JsonTypeInfoSerializerFactory.RegisterSerializableType<TData>();
        JsonTypeInfoSerializerFactory.RegisterSerializableType<IAsyncEnumerable<TData>>();
        DefaultQueryOrderer.RegisterDefaultKeySelectors<TData>();
        var invoker = typeof(TData) == typeof(int)
            ? (EndpointInvoker<TData, TId>)Activator.CreateInstance(typeof(EndpointInvokerInt32<>).MakeGenericType(typeof(TData)))!
            : typeof(TData) == typeof(string)
                ? (EndpointInvoker<TData, TId>)Activator.CreateInstance(typeof(EndpointInvokerString<>).MakeGenericType(typeof(TData)))!
                : typeof(TData) == typeof(Guid)
                    ? (EndpointInvoker<TData, TId>)Activator.CreateInstance(typeof(EndpointInvokerGuid<>).MakeGenericType(typeof(TData)))!
                    : new EndpointInvoker<TData, TId>();
        Invokers.Add(typeof(TData), invoker);
        return AddInternal(typeof(TData), name);
    }

    public RestEntitiesConfigurationBuilder Add<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TData, TId>()
        where TData : class, IHasId<TId>
        => Add<TData, TId>(typeof(TData).Name.ToLowerInvariant());

#if NET8_0_OR_GREATER
    public RestEntitiesConfiguration Build() => new(
        EntityNames.ToFrozenDictionary(),
        EntityTypes.ToFrozenDictionary(),
        Invokers.ToFrozenDictionary()
    );
#else
    public RestEntitiesConfiguration Build() => new(
        EntityNames.ToImmutableDictionary(),
        EntityTypes.ToImmutableDictionary(),
        Invokers.ToImmutableDictionary()
    );
#endif
}