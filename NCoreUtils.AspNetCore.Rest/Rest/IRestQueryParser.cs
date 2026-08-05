using Microsoft.AspNetCore.Http;

namespace NCoreUtils.AspNetCore.Rest;

public interface IRestQueryParser
{
    ValueTask<RestQuery> ParseAsync(HttpRequest httpRequest, CancellationToken cancellationToken);
}