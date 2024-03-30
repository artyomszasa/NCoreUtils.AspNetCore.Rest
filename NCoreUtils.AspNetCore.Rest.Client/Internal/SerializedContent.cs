using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace NCoreUtils.Rest.Internal;

public sealed class SerializedContent<T> : HttpContent
{
    private static readonly MediaTypeHeaderValue ApplicationJson = MediaTypeHeaderValue.Parse("application/json; charset=utf-8");

    public T Value { get; }

    public ISerializer<T> Serializer { get; }

    public SerializedContent(T value, ISerializer<T> serializer)
    {
        Value = value;
        Serializer = serializer;
        Headers.ContentType = serializer.ContentType switch
        {
            null or "application/json" or "application/json; charset=utf-8" => ApplicationJson,
            var rawContentType => MediaTypeHeaderValue.Parse(rawContentType)
        };
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        => Serializer.SerializeAsync(stream, Value).AsTask();

    protected override bool TryComputeLength(out long length)
    {
        length = default;
        return false;
    }
}