using System;
using System.Diagnostics.CodeAnalysis;

namespace NCoreUtils.AspNetCore.Rest
{
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

        public RestConfigurationBuilder AddEntity([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, CaseInsensitive name)
        {
            EntitiesConfiguration.Add(type, name);
            return this;
        }

        public RestConfigurationBuilder AddEntity([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
        {
            EntitiesConfiguration.Add(type);
            return this;
        }

        public RestConfigurationBuilder AddEntity<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CaseInsensitive name)
            => AddEntity(typeof(T), name);

        public RestConfigurationBuilder AddEntity<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>()
            => AddEntity(typeof(T));

        // public RestConfigurationBuilder AddEntityRange(params Type[] types)
        // {
        //     EntitiesConfiguration.AddRange(types);
        //     return this;
        // }

        public RestConfiguration Build()
            => new RestConfiguration(
                Prefix,
                AccessConfiguration.Build(),
                EntitiesConfiguration.Build()
            );
    }
}