using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using NCoreUtils.Rest.Internal;

namespace NCoreUtils.Rest;

[Obsolete("Use RestClientJsonTypeInfoResolver")]
public class RestClientJsonSerializerContext : IRestClientJsonSerializerContext, IRestClientJsonTypeInfoResolver
{
    public JsonSerializerContext JsonSerializerContext { get; }

    public RestClientJsonSerializerContext(JsonSerializerContext jsonSerializerContext)
        => JsonSerializerContext = jsonSerializerContext ?? throw new ArgumentNullException(nameof(jsonSerializerContext));

    public JsonTypeInfo? GetTypeInfo(Type type)
        => JsonSerializerContext.GetTypeInfo(type);

    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
        => JsonSerializerContext.GetTypeInfo(type);
}