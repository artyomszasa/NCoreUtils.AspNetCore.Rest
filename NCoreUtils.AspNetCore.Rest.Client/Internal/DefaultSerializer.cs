using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Rest.Internal;

public class DefaultSerializer<T>(string contentType, JsonSerializerContext jsonSerializerContext) : ISerializer<T>
{
    public string ContentType { get; } = contentType;

    public JsonSerializerContext JsonSerializerContext { get; } = jsonSerializerContext ?? throw new ArgumentNullException(nameof(jsonSerializerContext));

    public JsonTypeInfo<T> JsonTypeInfo { get; } = jsonSerializerContext.GetTypeInfo(typeof(T)) switch
    {
        null => throw new ArgumentException($"Specified json serializer info does not contain type info for {typeof(T)}."),
        JsonTypeInfo<T> jsonTypeInfo => jsonTypeInfo,
        _ => throw new ArgumentException($"Specified json serializer info contains invalid type info for {typeof(T)}."),
    };

    public ValueTask<T> DeserializeAsync(Stream stream, CancellationToken cancellationToken = default)
        => JsonSerializer.DeserializeAsync(stream, JsonTypeInfo, cancellationToken)!;

    public ValueTask SerializeAsync(Stream stream, T value, CancellationToken cancellationToken = default)
        => new(JsonSerializer.SerializeAsync(stream, value, JsonTypeInfo, cancellationToken));

    public async IAsyncEnumerable<T> DeserializeAsyncEnumerable(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable(stream, JsonTypeInfo, cancellationToken).ConfigureAwait(false))
        {
            if (item is not null)
            {
                yield return item;
            }
        }
    }
}