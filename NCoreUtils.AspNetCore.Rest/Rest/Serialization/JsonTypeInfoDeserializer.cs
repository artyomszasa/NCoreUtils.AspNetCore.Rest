using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace NCoreUtils.AspNetCore.Rest.Serialization;

public class JsonTypeInfoDeserializer<T>(JsonTypeInfo<T> typeInfo) : IDeserializer<T>
{
    public JsonTypeInfo<T> TypeInfo { get; } = typeInfo ?? throw new ArgumentNullException(nameof(typeInfo));

    public ValueTask<T> DeserializeAsync(Stream stream, CancellationToken cancellationToken)
        => JsonSerializer.DeserializeAsync(stream, TypeInfo, cancellationToken)!;
}