using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NCoreUtils.AspNetCore.Rest.Serialization;

namespace NCoreUtils.AspNetCore.Rest;

[Obsolete("Compatibility only.")]
public class RestConfigurationBuilder
{
    public string Prefix { get; set; } = string.Empty;

    public RestAccessConfigurationBuilder AccessConfiguration { get; } = new RestAccessConfigurationBuilder();

    public RestEntitiesConfigurationBuilder EntitiesConfiguration { get; } = new RestEntitiesConfigurationBuilder();

    public RestConfigurationBuilder WithPrefix(string prefix)
    {
        Prefix = prefix;
        return this;
    }

    public RestConfigurationBuilder ConfigureAccess(Action<RestAccessConfigurationBuilder> configure)
    {
        configure(AccessConfiguration);
        return this;
    }

    public RestConfigurationBuilder ConfigureEntities(Action<RestEntitiesConfigurationBuilder> configure)
    {
        configure(EntitiesConfiguration);
        return this;
    }

    [Obsolete("When using this method JsonTypeInfoSerializerFactory.RegisterSerializableType must be called manually.")]
    public RestConfigurationBuilder AddEntity([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, CaseInsensitive name)
    {
        EntitiesConfiguration.Add(type, name);
        return this;
    }

    [Obsolete("When using this method JsonTypeInfoSerializerFactory.RegisterSerializableType must be called manually.")]
    public RestConfigurationBuilder AddEntity([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
    {
        EntitiesConfiguration.Add(type);
        return this;
    }

    public RestConfigurationBuilder AddEntity<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CaseInsensitive name)
    {
        EntitiesConfiguration.Add(typeof(T), name);
        JsonTypeInfoSerializerFactory.RegisterSerializableType<T>();
        JsonTypeInfoSerializerFactory.RegisterSerializableType<IAsyncEnumerable<T>>();
        return this;
    }

    public RestConfigurationBuilder AddEntity<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>()
        => AddEntity<T>(typeof(T).Name);

    public RestConfiguration Build() => new(
        Prefix,
        AccessConfiguration.Build(),
        EntitiesConfiguration.Build()
    );
}