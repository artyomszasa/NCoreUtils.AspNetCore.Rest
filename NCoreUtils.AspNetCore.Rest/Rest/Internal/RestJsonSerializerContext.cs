using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace NCoreUtils.AspNetCore.Rest.Internal;

[Obsolete("Use RestJsonTypeInfoResolver")]
public class RestJsonSerializerContext(JsonSerializerContext jsonSerializerContext) : IRestJsonSerializerContext, IRestJsonTypeInfoResolver
{
    public JsonSerializerContext JsonSerializerContext { get; } = jsonSerializerContext
            ?? throw new ArgumentNullException(nameof(jsonSerializerContext));

    public JsonSerializerOptions DefaultOptions => throw new NotImplementedException();

    public JsonTypeInfo? GetTypeInfo(Type type)
        => JsonSerializerContext.GetTypeInfo(type);

    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
        => JsonSerializerContext.GetTypeInfo(type);
}