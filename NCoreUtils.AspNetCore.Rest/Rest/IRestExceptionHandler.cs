using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.AspNetCore.Rest;

public interface IRestExceptionHandler
{
#pragma warning disable CA1716 // Identifiers should not match keywords
    ValueTask<RestExceptionHandlerResult> HandleAsync(
        IServiceProvider serviceProvider,
        HttpResponse response,
        ILogger logger,
        ExceptionDispatchInfo error,
        CancellationToken cancellationToken = default
    );
#pragma warning restore CA1716 // Identifiers should not match keywords
}