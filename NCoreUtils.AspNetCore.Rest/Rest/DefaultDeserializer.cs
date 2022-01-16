using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.AspNetCore.Rest
{
    [Obsolete("JsonSerializerContext based seriialization is preferred.")]
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
    public class DefaultDeserializer<T> : IDeserializer<T>
    {
        readonly JsonSerializerOptions _jsonOptions;

        public DefaultDeserializer(JsonSerializerOptions jsonOptions)
        {
            _jsonOptions = jsonOptions ?? throw new ArgumentNullException(nameof(jsonOptions));
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Obsolete method.")]
        [UnconditionalSuppressMessage("Trimming", "IL2046", Justification = "Obsolete method.")]
        public ValueTask<T> DeserializeAsync(Stream stream, CancellationToken cancellationToken)
            => JsonSerializer.DeserializeAsync<T>(stream, _jsonOptions, cancellationToken)!;
    }
}