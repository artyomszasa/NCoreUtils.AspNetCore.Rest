using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Rest.Internal
{
    public class DefaultSerializer<T> : ISerializer<T>
    {
        public string ContentType { get; }

        public JsonSerializerOptions JsonOptions { get; }

        public DefaultSerializer(string contentType, JsonSerializerOptions? jsonOptions = default)
        {
            ContentType = contentType;
            JsonOptions = jsonOptions ?? DefaultSerializerFactory.DefaultJsonOptions;
        }

        public ValueTask<T> DeserializeAsync(Stream stream, CancellationToken cancellationToken = default)
            => JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);

        public ValueTask SerializeAsync(Stream stream, T value, CancellationToken cancellationToken = default)
            => new ValueTask(JsonSerializer.SerializeAsync<T>(stream, value, JsonOptions, cancellationToken));
    }
}