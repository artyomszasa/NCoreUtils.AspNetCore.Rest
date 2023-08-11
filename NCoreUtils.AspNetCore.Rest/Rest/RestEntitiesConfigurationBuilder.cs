using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using NCoreUtils.AspNetCore.Rest.Serialization;

namespace NCoreUtils.AspNetCore.Rest;

public class RestEntitiesConfigurationBuilder
{
    readonly Dictionary<Type, string> _entityNames = new();

    readonly Dictionary<CaseInsensitive, Type> _entityTypes = new();

    private RestEntitiesConfigurationBuilder AddInternal(Type type, CaseInsensitive name)
    {
        if (_entityNames.ContainsKey(type))
        {
            throw new InvalidOperationException($"{type} has already been registered.");
        }
        if (_entityTypes.TryGetValue(name, out var xtype))
        {
            throw new InvalidOperationException($"{xtype} has already been registered with name = {name}.");
        }
        _entityNames.Add(type, name.ToLowerString());
        _entityTypes.Add(name, type);
        return this;
    }

    [Obsolete("When using this method JsonTypeInfoSerializerFactory.RegisterSerializableType must be called manually")]
    public RestEntitiesConfigurationBuilder Add(Type type, CaseInsensitive name)
        => AddInternal(type, name);

    [Obsolete("When using this method JsonTypeInfoSerializerFactory.RegisterSerializableType must be called manually")]
    public RestEntitiesConfigurationBuilder Add(Type type)
        => AddInternal(type, type.Name.ToLowerInvariant());

    public RestEntitiesConfigurationBuilder Add<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>(CaseInsensitive name)
    {
        JsonTypeInfoSerializerFactory.RegisterSerializableType<T>();
        JsonTypeInfoSerializerFactory.RegisterSerializableType<IAsyncEnumerable<T>>();
        return AddInternal(typeof(T), name);
    }

    public RestEntitiesConfigurationBuilder Add<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>()
        => Add<T>(typeof(T).Name.ToLowerInvariant());

    [Obsolete("When using this method JsonTypeInfoSerializerFactory.RegisterSerializableType must be called manually")]
    public RestEntitiesConfigurationBuilder AddRange(params Type[] types)
    {
        foreach (var type in types)
        {
            Add(type);
        }
        return this;
    }

    public RestEntitiesConfiguration Build() => new(
        _entityNames.ToImmutableDictionary(),
        _entityTypes.ToImmutableDictionary()
    );
}