#if NET6_0_OR_GREATER
using System.Runtime.Serialization;
#endif

namespace NCoreUtils.Rest;

#if !NET8_0_OR_GREATER
[Serializable]
#endif
public class RestException : Exception
{
#if !NET8_0_OR_GREATER
    private const string KeyUri = "RestUri";
#endif

    [SuppressMessage("Design", "CA1056:URI-like properties should not be strings", Justification = "Only used as a string.")]
    public string Uri { get; }

    public RestException(string uri, string message, Exception innerException)
        : base(message, innerException)
        => Uri = uri ?? throw new ArgumentNullException(nameof(uri));

    public RestException(string uri, string message)
        : base(message)
        => Uri = uri ?? throw new ArgumentNullException(nameof(uri));

    public RestException()
        => Uri = string.Empty;

    public RestException(string message) : base(message)
        => Uri = string.Empty;

    public RestException(string message, Exception innerException) : base(message, innerException)
        => Uri = string.Empty;

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