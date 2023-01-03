using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Rest.Internal;

public class TypedSerializer<T> : ISerializer<T>
{
    public string ContentType { get; }

    public JsonTypeInfo<T> JsonTypeInfo { get; }

    public TypedSerializer(string contentType, JsonTypeInfo<T> jsonTypeInfo)
    {
        ContentType = contentType;
        JsonTypeInfo = jsonTypeInfo ?? throw new ArgumentNullException(nameof(jsonTypeInfo));
    }

    public ValueTask<T> DeserializeAsync(Stream stream, CancellationToken cancellationToken = default)
            => JsonSerializer.DeserializeAsync(stream, JsonTypeInfo, cancellationToken)!;

    public ValueTask SerializeAsync(Stream stream, T value, CancellationToken cancellationToken = default)
        => new ValueTask(JsonSerializer.SerializeAsync(stream, value, JsonTypeInfo, cancellationToken));

#if NET7_0_OR_GREATER
    public IAsyncEnumerable<T> DeserializeAsyncEnumerable(Stream stream, CancellationToken cancellationToken = default)
        => JsonSerializer.DeserializeAsyncEnumerable(stream, JsonTypeInfo, cancellationToken)
            .Where(item => item is not null)!;
#endif
}