using System;
using System.Text.Json.Serialization;

namespace NCoreUtils.AspNetCore.Rest.Serialization;

[Obsolete("Use JsonTypeInfoDeserializer instead.")]
public class JsonSerializerContextDeserializer<T>(JsonSerializerContext jsonSerializerContext) : JsonTypeInfoDeserializer<T>(jsonSerializerContext.GetJsonTypeInfoOrThrow<T>())
{
    public JsonSerializerContext JsonSerializerContext { get; } = jsonSerializerContext ?? throw new ArgumentNullException(nameof(jsonSerializerContext));
}