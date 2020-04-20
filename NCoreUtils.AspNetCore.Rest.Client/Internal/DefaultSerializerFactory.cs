using System;
using System.Text.Json;

namespace NCoreUtils.Rest.Internal
{
    public class DefaultSerializerFactory : ISerializerFactory
    {
        public static JsonSerializerOptions DefaultJsonOptions { get; } = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public JsonSerializerOptions JsonOptions { get; }

        public string ContentType { get; } = "application/json; charset=utf-8";

        public DefaultSerializerFactory(JsonSerializerOptions? jsonOptions)
            => JsonOptions = jsonOptions ?? DefaultJsonOptions;

        public ISerializer<T> GetSerializer<T>() => new DefaultSerializer<T>(ContentType, JsonOptions);
    }
}