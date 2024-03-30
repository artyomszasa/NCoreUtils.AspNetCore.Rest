using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using NCoreUtils.Data;
#if NET8_0_OR_GREATER
using ConfigurationDictionary = System.Collections.Frozen.FrozenDictionary<
    System.Type,
    NCoreUtils.Rest.Internal.IRestClientContextConfiguration
>;
#else
using ConfigurationDictionary = System.Collections.Immutable.ImmutableDictionary<
    System.Type,
    NCoreUtils.Rest.Internal.IRestClientContextConfiguration
>;
#endif

namespace NCoreUtils.Rest.Internal;

public interface IRestClientContextConfiguration
{
    string Endpoint { get; }

    string HttpClientConfigurationName { get; }

    TypedRestClient CreateClient(RestClientContextFactory factory);
}

public interface IRestClientContextConfiguration<TData, TId> : IRestClientContextConfiguration
    where TData : class, IHasId<TId>
    where TId : IEquatable<TId>
{
    IRestIdHandler<TId> IdHandler { get; }

    TypedRestClient IRestClientContextConfiguration.CreateClient(RestClientContextFactory factory)
        => new TypedRestClient<TData, TId>(
            factory.LoggerFactory.CreateLogger<TypedRestClient<TData, TId>>(),
            new RestClientContext<TData, TId>(
                factory.HttpClientFactory,
                factory.SerializerFactory.GetSerializer<TData>(),
                factory.QuerySerializer,
                IdHandler,
                Endpoint,
                HttpClientConfigurationName
            )
        );
}

public class RestClientContextFactory(
    ConfigurationDictionary configurations,
    ILoggerFactory loggerFactory,
    IHttpClientFactory httpClientFactory,
    IRestQuerySerializer querySerializer,
    ISerializerFactory serializerFactory)
{
    private ConfigurationDictionary Configurations { get; } = configurations ?? throw new ArgumentNullException(nameof(configurations));

    public ILoggerFactory LoggerFactory { get; } = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

    public IHttpClientFactory HttpClientFactory { get; } = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

    public IRestQuerySerializer QuerySerializer { get; } = querySerializer ?? throw new ArgumentNullException(nameof(querySerializer));

    public ISerializerFactory SerializerFactory { get; } = serializerFactory ?? throw new ArgumentNullException(nameof(serializerFactory));

    public TypedRestClient CreateClient(Type type)
        => Configurations.TryGetValue(type, out var configuration)
            ? configuration.CreateClient(this)
            : throw new InvalidOperationException($"No context configuration registered for {type}.");
}