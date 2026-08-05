using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.AspNetCore.Rest;

public interface IRestExceptionHandler
{
    [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "Intentional.")]
    ValueTask<RestExceptionHandlerResult> HandleAsync(
        IServiceProvider serviceProvider,
        HttpResponse response,
        ILogger logger,
        ExceptionDispatchInfo error,
        CancellationToken cancellationToken = default
    );
}