using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

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

    private JsonSerializerOptions? AsyncEnumerableSerializerOptions { get; set; }

    public JsonSerializerContext JsonSerializerContext { get; }

    public virtual string ContentType { get; } = "application/json; charset=utf-8";

    public DefaultSerializerFactory(JsonSerializerContext jsonSerializerContext)
        => JsonSerializerContext = jsonSerializerContext ?? throw new ArgumentNullException(nameof(jsonSerializerContext));

    public ISerializer<T> GetSerializer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>() => IsAsyncEnumerable(typeof(T), out var elementType)
        ? (ISerializer<T>)Activator.CreateInstance(
            typeof(JsonContextBackedSerializer<>).MakeGenericType(elementType),
            new object[]
            {
                AsyncEnumerableSerializerOptions ??= new() { Converters = { new JsonContextBackedConverterFactory(JsonSerializerContext) } }
            }
        )!
        : new DefaultSerializer<T>(ContentType, JsonSerializerContext);
}