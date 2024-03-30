using System;
using System.Runtime.Serialization;

namespace NCoreUtils.Rest;

#if !NET8_0_OR_GREATER
[Serializable]
#endif
public class RestException : Exception
{
#if !NET8_0_OR_GREATER
    private const string KeyUri = "RestUri";
#endif

    public string Uri { get; }

    public RestException(string uri, string message, Exception innerException)
        : base(message, innerException)
        => Uri = uri ?? throw new ArgumentNullException(nameof(uri));

    public RestException(string uri, string message)
        : base(message)
        => Uri = uri ?? throw new ArgumentNullException(nameof(uri));

#if !NET8_0_OR_GREATER
    protected RestException(SerializationInfo info, StreamingContext context)
        : base(info, context)
        => Uri = info.GetString(KeyUri) ?? string.Empty;

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(KeyUri, Uri);
    }
#endif
}