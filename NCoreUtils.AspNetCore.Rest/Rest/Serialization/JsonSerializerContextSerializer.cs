using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.AspNetCore.Rest.Serialization;

public class JsonSerializerContextSerializer<T> : ISerializer<T>
{
    public JsonSerializerContext JsonSerializerContext { get; }

    public JsonTypeInfo<T> JsonTypeInfo { get; }

    public JsonSerializerContextSerializer(JsonSerializerContext jsonSerializerContext)
    {
        JsonSerializerContext = jsonSerializerContext ?? throw new ArgumentNullException(nameof(jsonSerializerContext));
        switch (jsonSerializerContext.GetTypeInfo(typeof(T)))
        {
            case null:
                throw new ArgumentException($"Specified json serializer info does not contain type info for {typeof(T)}.");
            case JsonTypeInfo<T> jsonTypeInfo:
                JsonTypeInfo = jsonTypeInfo;
                break;
            default:
                throw new ArgumentException($"Specified json serializer info contains invalid type info for {typeof(T)}.");
        }
    }

    public async ValueTask SerializeAsync(IConfigurableOutput<Stream> configurableStream, T item, CancellationToken cancellationToken = default)
    {
        using var stream = await configurableStream.InitializeAsync(new OutputInfo(default, "application/json; charset=utf-8"), cancellationToken);
        await JsonSerializer.SerializeAsync(stream, item, JsonTypeInfo, cancellationToken);
    }
}