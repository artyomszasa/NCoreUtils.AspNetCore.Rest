using System;
using System.Text.Json.Serialization;

namespace NCoreUtils.AspNetCore.Rest.Serialization;

[Obsolete("Use JsonTypeInfoSerializer instead.")]
public class JsonSerializerContextSerializer<T>(JsonSerializerContext jsonSerializerContext)
    : JsonTypeInfoSerializer<T>(jsonSerializerContext.GetJsonTypeInfoOrThrow<T>())
{
    public JsonSerializerContext JsonSerializerContext { get; } = jsonSerializerContext ?? throw new ArgumentNullException(nameof(jsonSerializerContext));
}