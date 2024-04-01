using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using NCoreUtils.Data;
using NCoreUtils.Data.Protocol.Linq;

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

public class RestClientContextFactory(
    ConfigurationDictionary configurations,
    ILoggerFactory loggerFactory,
    IHttpClientFactory httpClientFactory,
    IRestQuerySerializer querySerializer,
    ISerializerFactory serializerFactory,
    IProtocolQueryProvider protocolQueryProvider)
{
    private ConfigurationDictionary Configurations { get; } = configurations ?? throw new ArgumentNullException(nameof(configurations));

    public ILoggerFactory LoggerFactory { get; } = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

    public IHttpClientFactory HttpClientFactory { get; } = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

    public IRestQuerySerializer QuerySerializer { get; } = querySerializer ?? throw new ArgumentNullException(nameof(querySerializer));

    public ISerializerFactory SerializerFactory { get; } = serializerFactory ?? throw new ArgumentNullException(nameof(serializerFactory));

    public IProtocolQueryProvider ProtocolQueryProvider { get; } = protocolQueryProvider ?? throw new ArgumentNullException(nameof(protocolQueryProvider));

    public TypedRestClient CreateClient(Type type)
        => Configurations.TryGetValue(type, out var configuration)
            ? configuration.CreateClient(this)
            : throw new InvalidOperationException($"No context configuration registered for {type}.");
}