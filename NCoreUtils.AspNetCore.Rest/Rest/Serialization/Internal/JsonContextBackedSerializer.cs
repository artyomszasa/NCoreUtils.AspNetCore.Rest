using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.AspNetCore.Rest.Serialization.Internal;

public sealed class JsonContextBackedSerializer<T> : ISerializer<T>
{
    private JsonSerializerOptions Options { get; }

    public JsonContextBackedSerializer(JsonSerializerOptions options)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Item converter backed by JsonSerializerContext.")]
    public async ValueTask SerializeAsync(IConfigurableOutput<Stream> configurableStream, T item, CancellationToken cancellationToken = default)
    {
        using var stream = await configurableStream.InitializeAsync(new OutputInfo(default, "application/json; charset=utf-8"), cancellationToken);
        await JsonSerializer.SerializeAsync(stream, item, Options, cancellationToken);
    }
}