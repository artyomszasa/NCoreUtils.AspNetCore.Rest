using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Rest.Internal;

public class DefaultSerializerFactory : ISerializerFactory
{
    private static bool IsAsyncEnumerable(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type,
        [MaybeNullWhen(false)] out Type elementType)
    {
        if (type.IsInterface && type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
        {
            elementType = type.GetGenericArguments()[0];
            return true;
        }
        elementType = default;
        return false;
    }

    public ILogger Logger { get; }

    public JsonSerializerContext JsonSerializerContext { get; }

    public virtual string ContentType { get; } = "application/json; charset=utf-8";

    public DefaultSerializerFactory(ILogger<DefaultSerializerFactory> logger, JsonSerializerContext jsonSerializerContext)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        JsonSerializerContext = jsonSerializerContext ?? throw new ArgumentNullException(nameof(jsonSerializerContext));
    }

#pragma warning disable CS0618
    private ISerializer<T> CreateFallbackAsyncEnumerableSerializer<T>(Type elementType) => (ISerializer<T>)Activator.CreateInstance(
        typeof(JsonContextBackedSerializer<>).MakeGenericType(elementType),
        new object[]
        {
            new JsonSerializerOptions() { Converters = { new JsonContextBackedConverterFactory(JsonSerializerContext) } }
        }
    )!;
#pragma warning restore CS0618


    public ISerializer<T> GetSerializer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>()
        => JsonSerializerContext.GetTypeInfo(typeof(T)) switch
        {
            null when IsAsyncEnumerable(typeof(T), out var elementType) => CreateFallbackAsyncEnumerableSerializer<T>(elementType),
            null => throw new ArgumentException($"Registered json serializer context does not contain type info for {typeof(T)}."),
            JsonTypeInfo<T> jsonTypeInfo => new TypedSerializer<T>(ContentType, jsonTypeInfo),
            _ => throw new ArgumentException($"Registered json serializer context returned invalid type info for {typeof(T)}.")
        };
}