using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Rest.Internal;

[Obsolete("For .NET7 or newer Typed Serializer should be used instead.")]
public sealed class JsonContextBackedSerializer<T> : ISerializer<IAsyncEnumerable<T>>
{
    private JsonSerializerOptions Options { get; }

    public string? ContentType => throw new NotImplementedException();

    public JsonContextBackedSerializer(JsonSerializerOptions options)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Item converter backed by JsonSerializerContext.")]
    public ValueTask<IAsyncEnumerable<T>> DeserializeAsync(Stream stream, CancellationToken cancellationToken = default)
        => new(JsonSerializer.DeserializeAsyncEnumerable<T>(stream, Options, cancellationToken)!);

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Item converter backed by JsonSerializerContext.")]
    public ValueTask SerializeAsync(Stream stream, IAsyncEnumerable<T> value, CancellationToken cancellationToken = default)
        => new(JsonSerializer.SerializeAsync(stream, value, Options, cancellationToken));

#if NET7_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Item converter backed by JsonSerializerContext.")]
    public IAsyncEnumerable<IAsyncEnumerable<T>> DeserializeAsyncEnumerable(Stream stream, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("Should never happen.");
#endif
}