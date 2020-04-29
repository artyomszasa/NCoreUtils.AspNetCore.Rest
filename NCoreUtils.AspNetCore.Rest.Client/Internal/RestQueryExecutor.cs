using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Data.Protocol.Ast;
using NCoreUtils.Data.Protocol.Linq;

namespace NCoreUtils.Rest.Internal
{
    public class RestQueryExecutor : IDataQueryExecutor
    {
        private static readonly Dictionary<string, string> _reductionMap = new Dictionary<string, string>
        {
            { nameof(Queryable.First), "first" },
            { nameof(Queryable.Single), "single" },
            { nameof(Queryable.FirstOrDefault), "first" },
            { nameof(Queryable.SingleOrDefault), "single" },
            { nameof(Queryable.Count), "count" }
        };

        private static HashSet<string> _throwIfNull = new HashSet<string>(new []
        {
            nameof(Queryable.First),
            nameof(Queryable.Single)
        });

        private static string AdaptReduction(string input)
            => _reductionMap.TryGetValue(input, out var replacement) ? replacement : input;

        readonly IHttpRestClient _client;

        public RestQueryExecutor(IHttpRestClient client)
            => _client = client ?? throw new ArgumentNullException(nameof(client));

        public IAsyncEnumerable<T> ExecuteEnumerationAsync<T>(
            string target,
            Node? filter = null,
            Node? sortBy = null,
            bool isDescending = false,
            int offset = 0,
            int limit = 0)
            => new DelayedAsyncEnumerable<T>(async cancellationToken =>
            {
                var results = await _client.ListCollectionAsync<T>(
                    target,
                    NodeModule.Stringify(filter),
                    NodeModule.Stringify(sortBy),
                    isDescending ? "desc" : "asc",
                    offset,
                    limit == 0 ? (int?)default : limit,
                    cancellationToken
                );
                return results.ToAsyncEnumerable();
            });

        public async Task<TResult> ExecuteReductionAsync<TSource, TResult>(
            string target,
            string reduction,
            Node? filter = null,
            Node? sortBy = null,
            bool isDescending = false,
            int offset = 0,
            int limit = 0,
            CancellationToken cancellationToken = default)
        {
            var result = await _client.ReductionAsync<TSource>(
                AdaptReduction(reduction),
                target,
                NodeModule.Stringify(filter),
                NodeModule.Stringify(sortBy),
                isDescending ? "desc" : "asc",
                offset,
                limit == 0 ? (int?)default : limit,
                cancellationToken
            );
            if (_throwIfNull.Contains(reduction) && result is null)
            {
                throw new InvalidOperationException("Sequence contains no elements");
            }
            return (TResult)result;
        }
    }
}