using System;
using System.Diagnostics.CodeAnalysis;

namespace NCoreUtils.AspNetCore.Rest;

public class RestEndpointsConfigurationBuilder
{
    public string Prefix { get; set; } = string.Empty;

    public RestEndpointsAccessConfigurationBuilder AccessConfiguration { get; } = new();

    public RestEntitiesConfigurationBuilder EntitiesConfiguration { get; } = new RestEntitiesConfigurationBuilder();

    public RestEndpointsConfigurationBuilder WithPrefix(string prefix)
    {
        Prefix = prefix;
        return this;
    }

    public RestEndpointsConfigurationBuilder ConfigureAccess(Action<RestEndpointsAccessConfigurationBuilder> configure)
    {
        configure(AccessConfiguration);
        return this;
    }

    public RestEndpointsConfigurationBuilder ConfigureEntities(Action<RestEntitiesConfigurationBuilder> configure)
    {
        configure(EntitiesConfiguration);
        return this;
    }

    public RestEndpointsConfigurationBuilder AddEntity<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CaseInsensitive name)
    {
        EntitiesConfiguration.Add<T>(name);
        return this;
    }

    public RestEndpointsConfigurationBuilder AddEntity<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>()
    {
        EntitiesConfiguration.Add<T>();
        return this;
    }

    public RestConfiguration Build() => new(
        Prefix,
        AccessConfiguration.Build(),
        EntitiesConfiguration.Build()
    );
}