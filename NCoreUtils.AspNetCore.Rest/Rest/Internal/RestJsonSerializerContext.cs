using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace NCoreUtils.AspNetCore.Rest.Internal;

[Obsolete("Use RestJsonTypeInfoResolver")]
public class RestJsonSerializerContext : IRestJsonSerializerContext, IRestJsonTypeInfoResolver
{
    public JsonSerializerContext JsonSerializerContext { get; }

    public JsonSerializerOptions DefaultOptions => throw new NotImplementedException();

    public RestJsonSerializerContext(JsonSerializerContext jsonSerializerContext)
        => JsonSerializerContext = jsonSerializerContext
            ?? throw new ArgumentNullException(nameof(jsonSerializerContext));

    public JsonTypeInfo? GetTypeInfo(Type type)
        => JsonSerializerContext.GetTypeInfo(type);

    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
        => JsonSerializerContext.GetTypeInfo(type);
}