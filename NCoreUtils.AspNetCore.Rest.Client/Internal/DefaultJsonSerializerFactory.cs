using System;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Rest.Internal;

public class DefaultJsonSerializerFactory(ILogger<DefaultJsonSerializerFactory> logger, IRestClientJsonTypeInfoResolver resolver) : ISerializerFactory
{
    public ILogger Logger { get; } = logger ?? throw new ArgumentNullException(nameof(logger));

    public IRestClientJsonTypeInfoResolver Resolver { get; } = resolver ?? throw new ArgumentNullException(nameof(resolver));

    public virtual string ContentType { get; } = "application/json; charset=utf-8";

    public ISerializer<T> GetSerializer<T>()
        => Resolver.GetTypeInfo(typeof(T)) switch
        {
            null => throw new ArgumentException($"Registered json type info resolver does not contain type info for {typeof(T)}."),
            JsonTypeInfo<T> jsonTypeInfo => new JsonTypeInfoSerializer<T>(ContentType, jsonTypeInfo),
            _ => throw new ArgumentException($"Registered json type info resolver returned invalid type info for {typeof(T)}.")
        };
}