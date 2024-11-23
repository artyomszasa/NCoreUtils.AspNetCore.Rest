using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Rest;

public interface IRestClientErrorHandler
{
    ValueTask ProcessAsync(
        HttpResponseMessage response,
        string requestUri,
        CancellationToken cancellationToken = default
    );
}