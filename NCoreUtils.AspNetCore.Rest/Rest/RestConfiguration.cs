namespace NCoreUtils.AspNetCore.Rest;

public class RestConfiguration(string prefix, RestAccessConfiguration accessConfiguration, RestEntitiesConfiguration entitiesConfiguration)
{
    public string Prefix { get; } = prefix ?? throw new ArgumentNullException(nameof(prefix));

    public RestAccessConfiguration AccessConfiguration { get; } = accessConfiguration ?? throw new ArgumentNullException(nameof(accessConfiguration));

    public RestEntitiesConfiguration EntitiesConfiguration { get; } = entitiesConfiguration ?? throw new ArgumentNullException(nameof(entitiesConfiguration));
}