using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.AspNetCore.Rest.Serialization;

public class JsonTypeInfoSerializer<T> : ISerializer<T>
{
    public JsonTypeInfo<T> TypeInfo { get; }

    public JsonTypeInfoSerializer(JsonTypeInfo<T> typeInfo)
        => TypeInfo = typeInfo ?? throw new ArgumentNullException(nameof(typeInfo));

    public async ValueTask SerializeAsync(IConfigurableOutput<Stream> configurableStream, T item, CancellationToken cancellationToken = default)
    {
        await using var stream = await configurableStream
            .InitializeAsync(new OutputInfo(default, "application/json; charset=utf-8"), cancellationToken)
            .ConfigureAwait(false);
        await JsonSerializer.SerializeAsync(stream, item, TypeInfo, cancellationToken).ConfigureAwait(false);
    }
}