using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NCoreUtils.Rest.Internal
{
    public class DefaultSerializerFactory : ISerializerFactory
    {
        public JsonSerializerContext JsonSerializerContext { get; }

        public string ContentType { get; } = "application/json; charset=utf-8";

        public DefaultSerializerFactory(JsonSerializerContext jsonSerializerContext)
            => JsonSerializerContext = jsonSerializerContext ?? throw new ArgumentNullException(nameof(jsonSerializerContext));

        public ISerializer<T> GetSerializer<T>() => new DefaultSerializer<T>(ContentType, JsonSerializerContext);
    }
}