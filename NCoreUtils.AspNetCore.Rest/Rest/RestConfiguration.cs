using System;

namespace NCoreUtils.AspNetCore.Rest
{
    public class RestConfiguration
    {
        public string Prefix { get; }

        public RestAccessConfiguration AccessConfiguration { get; }

        public RestEntitiesConfiguration EntitiesConfiguration { get; }

        public RestConfiguration(string prefix, RestAccessConfiguration accessConfiguration, RestEntitiesConfiguration entitiesConfiguration)
        {
            Prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
            AccessConfiguration = accessConfiguration ?? throw new ArgumentNullException(nameof(accessConfiguration));
            EntitiesConfiguration = entitiesConfiguration ?? throw new ArgumentNullException(nameof(entitiesConfiguration));
        }
    }
}