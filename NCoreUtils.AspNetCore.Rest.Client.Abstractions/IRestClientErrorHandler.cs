using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Rest;

public interface IRestClientErrorHandler
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1054:Uri parameters should not be strings",
        Justification = "Maintaining backward compatibility in public API.")]
    ValueTask ProcessAsync(
        HttpResponseMessage response,
        string requestUri,
        CancellationToken cancellationToken = default
    );
}