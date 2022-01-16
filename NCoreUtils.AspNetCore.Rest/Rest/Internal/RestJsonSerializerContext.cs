using System;
using System.Text.Json.Serialization;

namespace NCoreUtils.AspNetCore.Rest.Internal;

public class RestJsonSerializerContext : IRestJsonSerializerContext
{
    public JsonSerializerContext JsonSerializerContext { get; }

    public RestJsonSerializerContext(JsonSerializerContext jsonSerializerContext)
        => JsonSerializerContext = jsonSerializerContext
            ?? throw new ArgumentNullException(nameof(jsonSerializerContext));
}