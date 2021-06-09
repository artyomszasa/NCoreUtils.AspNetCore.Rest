using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NCoreUtils.AspNetCore.Rest.QueryParsers;

namespace NCoreUtils.AspNetCore.Rest
{
    public interface IRestQueryParser
    {
        ValueTask<RestQuery> ParseAsync(HttpRequest httpRequest, CancellationToken cancellationToken);
    }
}