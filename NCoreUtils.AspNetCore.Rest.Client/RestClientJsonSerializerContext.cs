using System;
using System.Text.Json.Serialization;
using NCoreUtils.Rest.Internal;

namespace NCoreUtils.Rest;

public class RestClientJsonSerializerContext : IRestClientJsonSerializerContext
{
    public JsonSerializerContext JsonSerializerContext { get; }

    public RestClientJsonSerializerContext(JsonSerializerContext jsonSerializerContext)
        => JsonSerializerContext = jsonSerializerContext ?? throw new ArgumentNullException(nameof(jsonSerializerContext));
}