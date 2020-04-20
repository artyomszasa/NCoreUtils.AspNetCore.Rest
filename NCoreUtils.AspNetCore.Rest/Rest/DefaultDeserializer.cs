using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.AspNetCore.Rest
{
    public class DefaultDeserializer<T> : IDeserializer<T>
    {
        readonly JsonSerializerOptions _jsonOptions;

        public DefaultDeserializer(JsonSerializerOptions jsonOptions)
        {
            _jsonOptions = jsonOptions ?? throw new ArgumentNullException(nameof(jsonOptions));
        }

        public ValueTask<T> DeserializeAsync(Stream stream, CancellationToken cancellationToken)
            => JsonSerializer.DeserializeAsync<T>(stream, _jsonOptions, cancellationToken);
    }
}