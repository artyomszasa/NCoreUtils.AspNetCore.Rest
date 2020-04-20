using System;
using System.Runtime.Serialization;

namespace NCoreUtils.Rest
{
    [Serializable]
    public class RestException : Exception
    {
        private const string KeyUri = "RestUri";

        public string Uri { get; }

        public RestException(string uri, string message, Exception innerException)
            : base(message, innerException)
            => Uri = uri ?? throw new ArgumentNullException(nameof(uri));

        public RestException(string uri, string message)
            : base(message)
            => Uri = uri ?? throw new ArgumentNullException(nameof(uri));

        protected RestException(SerializationInfo info, StreamingContext context)
            : base(info, context)
            => Uri = info.GetString(KeyUri);

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(KeyUri, Uri);
        }
    }
}