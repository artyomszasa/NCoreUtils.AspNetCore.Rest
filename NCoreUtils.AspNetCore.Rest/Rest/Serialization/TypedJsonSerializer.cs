using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.AspNetCore.Rest.Serialization;

public class TypedJsonSerializer<T> : ISerializer<T>
{
    public JsonTypeInfo<T> JsonTypeInfo { get; }

    public TypedJsonSerializer(JsonTypeInfo<T> jsonTypeInfo)
        => JsonTypeInfo = jsonTypeInfo ?? throw new ArgumentNullException(nameof(jsonTypeInfo));

    public async ValueTask SerializeAsync(IConfigurableOutput<Stream> configurableStream, T item, CancellationToken cancellationToken = default)
    {
        using var stream = await configurableStream.InitializeAsync(new OutputInfo(default, "application/json; charset=utf-8"), cancellationToken);
        await JsonSerializer.SerializeAsync(stream, item, JsonTypeInfo, cancellationToken);
    }
}