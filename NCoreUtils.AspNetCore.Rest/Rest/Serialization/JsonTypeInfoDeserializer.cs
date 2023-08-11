using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.AspNetCore.Rest.Serialization;

public class JsonTypeInfoDeserializer<T> : IDeserializer<T>
{
    public JsonTypeInfo<T> TypeInfo { get; }

    public JsonTypeInfoDeserializer(JsonTypeInfo<T> typeInfo)
        => TypeInfo = typeInfo ?? throw new ArgumentNullException(nameof(typeInfo));

    public ValueTask<T> DeserializeAsync(Stream stream, CancellationToken cancellationToken)
        => JsonSerializer.DeserializeAsync(stream, TypeInfo, cancellationToken)!;
}