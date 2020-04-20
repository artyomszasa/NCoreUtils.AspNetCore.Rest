using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Data;

namespace NCoreUtils.Rest.Internal
{
    public static class HttpRestCientExtensions
    {
        public static Task UpdateAsync<TData, TId>(
            this IHttpRestClient client,
            TData data,
            CancellationToken cancellationToken = default)
            where TData : IHasId<TId>
            => client.UpdateAsync(data.Id, data, cancellationToken);
    }
}