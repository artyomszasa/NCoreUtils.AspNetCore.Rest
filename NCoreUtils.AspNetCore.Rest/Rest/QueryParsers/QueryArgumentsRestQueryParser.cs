using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NCoreUtils.Collections;

namespace NCoreUtils.AspNetCore.Rest.QueryParsers
{
    public class QueryArgumentsRestQueryParser : IRestQueryParser
    {
        public ValueTask<RestQuery> ParseAsync(HttpRequest httpRequest, CancellationToken cancellationToken)
        {
            var q = httpRequest.Query;
            // offset
            var offset = q.TryGetValue("offset", out var values)
                && values.Count > 0
                && int.TryParse(values[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ivalue)
                    ? (int?)ivalue
                    : default(int?);
            // count
            var count = q.TryGetValue("count", out values)
                && values.Count > 0
                && int.TryParse(values[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out ivalue)
                    ? (int?)ivalue
                    : default(int?);
            // filter
            var filter = q.TryGetValue("filter", out values) && values.Count > 0 ? values[0] : null;
            // fields
            using var fields = new ArrayPoolList<string>(4);
            if (q.TryGetValue("fields", out values) && values.Count > 0)
            {
                RestQueryParserHelpers.SplitCommaSeparatedStrings(values[0].AsSpan(), fields);
            }
            // sort by
            using var sortBy = new ArrayPoolList<string>(4);
            if (q.TryGetValue("sort-by", out values) && values.Count > 0)
            {
                RestQueryParserHelpers.SplitCommaSeparatedStrings(values[0].AsSpan(), sortBy);
            }
            // sort by direction
            using var sortByDirections = new ArrayPoolList<RestSortByDirection>(4);
            if (q.TryGetValue("sort-direction", out values) && values.Count > 0)
            {
                RestQueryParserHelpers.ParseSortByDirections(values[0].AsSpan(), sortByDirections);
            }
            // return rest query
            return new ValueTask<RestQuery>(new RestQuery(
                offset,
                count,
                filter,
                fields.Count == 0 ? default(ArraySegment<string>?) : fields.Disown(),
                sortBy.Count == 0 ? default(ArraySegment<string>?) : sortBy.Disown(),
                sortByDirections.Count == 0 ? default(ArraySegment<RestSortByDirection>?) : sortByDirections.Disown()
            ));
        }
    }
}