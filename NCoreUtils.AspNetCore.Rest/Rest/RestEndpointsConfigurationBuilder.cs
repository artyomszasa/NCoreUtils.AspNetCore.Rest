using System;
using System.Diagnostics.CodeAnalysis;
using NCoreUtils.Data;

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
        configure.ThrowIfNull()(AccessConfiguration);
        return this;
    }

    public RestEndpointsConfigurationBuilder ConfigureEntities(Action<RestEntitiesConfigurationBuilder> configure)
    {
        configure.ThrowIfNull()(EntitiesConfiguration);
        return this;
    }

    [Obsolete(RestEntitiesConfigurationBuilder.WarnEndpointInvoker)]
    public RestEndpointsConfigurationBuilder AddEntity<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CaseInsensitive name)
    {
        EntitiesConfiguration.Add<T>(name);
        return this;
    }

    [Obsolete(RestEntitiesConfigurationBuilder.WarnEndpointInvoker)]
    public RestEndpointsConfigurationBuilder AddEntity<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>()
    {
        EntitiesConfiguration.Add<T>();
        return this;
    }

    public RestEndpointsConfigurationBuilder AddEntity<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TData, TId>(CaseInsensitive name)
        where TData : class, IHasId<TId>
    {
        EntitiesConfiguration.Add<TData, TId>(name);
        return this;
    }

    public RestEndpointsConfigurationBuilder AddEntity<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TData, TId>()
        where TData : class, IHasId<TId>
    {
        EntitiesConfiguration.Add<TData, TId>();
        return this;
    }

    public RestConfiguration Build() => new(
        Prefix,
        AccessConfiguration.Build(),
        EntitiesConfiguration.Build()
    );
}