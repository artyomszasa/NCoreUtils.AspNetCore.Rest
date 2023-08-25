using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Rest.Internal;

public class DefaultJsonSerializerFactory : ISerializerFactory
{
    public ILogger Logger { get; }

    public IRestClientJsonTypeInfoResolver Resolver { get; }

    public virtual string ContentType { get; } = "application/json; charset=utf-8";

    public DefaultJsonSerializerFactory(ILogger<DefaultJsonSerializerFactory> logger, IRestClientJsonTypeInfoResolver resolver)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    public ISerializer<T> GetSerializer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>()
        => Resolver.GetTypeInfo(typeof(T)) switch
        {
            null => throw new ArgumentException($"Registered json type info resolver does not contain type info for {typeof(T)}."),
            JsonTypeInfo<T> jsonTypeInfo => new JsonTypeInfoSerializer<T>(ContentType, jsonTypeInfo),
            _ => throw new ArgumentException($"Registered json type info resolver returned invalid type info for {typeof(T)}.")
        };
}