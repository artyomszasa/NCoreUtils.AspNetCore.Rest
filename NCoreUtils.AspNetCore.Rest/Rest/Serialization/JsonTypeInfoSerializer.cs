using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace NCoreUtils.AspNetCore.Rest.Serialization;

public class JsonTypeInfoSerializer<T>(JsonTypeInfo<T> typeInfo) : ISerializer<T>
{
    public JsonTypeInfo<T> TypeInfo { get; } = typeInfo ?? throw new ArgumentNullException(nameof(typeInfo));

    public async ValueTask SerializeAsync(IConfigurableOutput<Stream> configurableStream, T item, CancellationToken cancellationToken = default)
    {
        Preconditions.ThrowIfNull(configurableStream);
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
        await using var stream = await configurableStream
            .InitializeAsync(new OutputInfo(default, "application/json; charset=utf-8"), cancellationToken)
            .ConfigureAwait(false);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
        await JsonSerializer.SerializeAsync(stream, item, TypeInfo, cancellationToken).ConfigureAwait(false);
    }
}