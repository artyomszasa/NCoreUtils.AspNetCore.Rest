using System;
using System.Text.Json.Serialization;

namespace NCoreUtils.AspNetCore.Rest.Serialization;

[Obsolete("Use JsonTypeInfoSerializer instead.")]
public class JsonSerializerContextSerializer<T> : JsonTypeInfoSerializer<T>
{
    public JsonSerializerContext JsonSerializerContext { get; }

    public JsonSerializerContextSerializer(JsonSerializerContext jsonSerializerContext)
        : base(jsonSerializerContext.GetJsonTypeInfoOrThrow<T>())
        => JsonSerializerContext = jsonSerializerContext ?? throw new ArgumentNullException(nameof(jsonSerializerContext));
}