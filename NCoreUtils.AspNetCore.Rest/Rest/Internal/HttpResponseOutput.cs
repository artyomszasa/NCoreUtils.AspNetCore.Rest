using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NCoreUtils.AspNetCore.Rest.Internal
{
    internal class HttpResponseOutput : IConfigurableOutput<Stream>
    {
        readonly HttpResponse _response;

        public HttpResponseOutput(HttpResponse response)
            => _response = response ?? throw new ArgumentNullException(nameof(response));

        public ValueTask<Stream> InitializeAsync(OutputInfo info, CancellationToken cancellationToken)
        {
            if (info.Length.HasValue)
            {
                _response.ContentLength = info.Length.Value;
            }
            if (!string.IsNullOrEmpty(info.ContentType))
            {
                _response.ContentType = info.ContentType;
            }
            return new ValueTask<Stream>(_response.Body);
        }
    }
}