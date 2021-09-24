using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NCoreUtils.Collections;

namespace NCoreUtils.AspNetCore.Rest.QueryParsers
{
    public class HeaderRestQueryParser : IRestQueryParser
    {
        public ValueTask<RestQuery> ParseAsync(HttpRequest httpRequest, CancellationToken cancellationToken)
        {
            var headers = httpRequest.Headers;
            // offset
            var offset = headers.TryGetValue("X-Offset", out var values)
                && values.Count > 0
                && int.TryParse(values[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ivalue)
                    ? (int?)ivalue
                    : default(int?);
            // count
            var count = headers.TryGetValue("X-Count", out values)
                && values.Count > 0
                && int.TryParse(values[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out ivalue)
                    ? (int?)ivalue
                    : default(int?);
            // filter
            var filter = headers.TryGetValue("X-Filter", out values) && values.Count > 0 ? Uri.UnescapeDataString(values[0]) : null;
            // sort by
            using var sortBy = new ArrayPoolList<string>(4);
            if (headers.TryGetValue("X-Sort-By", out values) && values.Count > 0)
            {
                RestQueryParserHelpers.SplitCommaSeparatedStrings(Uri.UnescapeDataString(values[0]).AsSpan(), sortBy);
            }
            // sort by direction
            using var sortByDirections = new ArrayPoolList<RestSortByDirection>(4);
            if (headers.TryGetValue("X-Sort-By-Direction", out values) && values.Count > 0)
            {
                RestQueryParserHelpers.ParseSortByDirections(values[0].AsSpan(), sortByDirections);
            }
            // return rest query
            return new ValueTask<RestQuery>(new RestQuery(
                offset,
                count,
                filter,
                default,
                sortBy.Count == 0 ? default(ArraySegment<string>?) : sortBy.Disown(),
                sortByDirections.Count == 0 ? default(ArraySegment<RestSortByDirection>?) : sortByDirections.Disown()
            ));
        }
    }
}