using System;
using System.Text.Json.Serialization;

namespace NCoreUtils.AspNetCore.Rest.Serialization;

[Obsolete("Use JsonTypeInfoDeserializer instead.")]
public class JsonSerializerContextDeserializer<T> : JsonTypeInfoDeserializer<T>
{
    public JsonSerializerContext JsonSerializerContext { get; }

    public JsonSerializerContextDeserializer(JsonSerializerContext jsonSerializerContext)
        : base(jsonSerializerContext.GetJsonTypeInfoOrThrow<T>())
        => JsonSerializerContext = jsonSerializerContext ?? throw new ArgumentNullException(nameof(jsonSerializerContext));
}