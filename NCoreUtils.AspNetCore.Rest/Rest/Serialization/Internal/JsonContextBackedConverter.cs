using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace NCoreUtils.AspNetCore.Rest.Serialization.Internal;

public sealed class JsonContextBackedConverter<T>(JsonTypeInfo<T> typeInfo) : JsonConverter<T>
{
    private JsonTypeInfo<T> TypeInfo { get; } = typeInfo ?? throw new ArgumentNullException(nameof(typeInfo));

    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => JsonSerializer.Deserialize(ref reader, TypeInfo);

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value, TypeInfo);
}