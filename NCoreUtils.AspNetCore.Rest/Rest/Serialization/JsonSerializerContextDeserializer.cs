using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.AspNetCore.Rest.Serialization;

public class JsonSerializerContextDeserializer<T> : IDeserializer<T>
{
    public JsonSerializerContext JsonSerializerContext { get; }

    public JsonTypeInfo<T> JsonTypeInfo { get; }

    public JsonSerializerContextDeserializer(JsonSerializerContext jsonSerializerContext)
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

    public ValueTask<T> DeserializeAsync(Stream stream, CancellationToken cancellationToken)
    {
        return JsonSerializer.DeserializeAsync(stream, JsonTypeInfo, cancellationToken)!;
    }
}