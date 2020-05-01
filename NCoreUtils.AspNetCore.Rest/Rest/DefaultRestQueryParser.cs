using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NCoreUtils.Collections;

namespace NCoreUtils.AspNetCore.Rest
{
    public class DefaultRestQueryParser : IRestQueryParser
    {
        static void SplitSortBy(ReadOnlySpan<char> input, ref TinyImmutableArray<string>.Builder builder)
        {
            var inString = false;
            var lastChar = '\0';
            var startIndex = 0;
            var i = 0;
            var l = input.Length;
            while (i < l)
            {
                var ch = input[i];
                switch (ch)
                {
                    case '"':
                        if (lastChar != '\\')
                        {
                            inString = !inString;
                        }
                        break;
                    case ',':
                        if (!inString)
                        {
                            builder.Add(input.Slice(startIndex, i - startIndex).ToString());
                            startIndex = i + 1;
                        }
                        break;
                }
                lastChar = ch;
                ++i;
            }
            if (startIndex < i)
            {
                if (inString)
                {
                    throw new FormatException($"Invalid sort by string: \"{input.ToString()}\".");
                }
                builder.Add(input.Slice(startIndex, i - startIndex).ToString());
            }
        }

        static void ParseSortByDirections(ReadOnlySpan<char> input, ref TinyImmutableArray<RestSortByDirection>.Builder builder)
        {
            var startIndex = 0;
            var i = 0;
            var l = input.Length;
            while (i < l)
            {
                switch (input[i])
                {
                    case ',':
                        builder.Add(Enum.Parse<RestSortByDirection>(input.Slice(startIndex, i - startIndex).ToString(), true));
                        startIndex = i + 1;
                        break;
                }
                ++i;
            }
            if (startIndex < i)
            {
                builder.Add(Enum.Parse<RestSortByDirection>(input.Slice(startIndex, i - startIndex).ToString(), true));
            }
        }

        public ValueTask<RestQuery> ParseAsync(HttpRequest httpRequest, CancellationToken cancellationToken)
        {
            var headers = httpRequest.Headers;
            // offset
            var offset = headers.TryGetValue("X-Offset", out var values)
                && values.Count > 0
                && int.TryParse(values[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ivalue)
                    ? ivalue
                    : 0;
            // count
            var count = headers.TryGetValue("X-Count", out values)
                && values.Count > 0
                && int.TryParse(values[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out ivalue)
                    ? ivalue
                    : 100000;
            // filter
            var filter = headers.TryGetValue("X-Filter", out values) && values.Count > 0 ? Uri.UnescapeDataString(values[0]) : null;
            // sort by
            var sortByBuilder = TinyImmutableArray.CreateBuilder<string>();
            if (headers.TryGetValue("X-Sort-By", out values) && values.Count > 0)
            {
                SplitSortBy(Uri.UnescapeDataString(values[0]).AsSpan(), ref sortByBuilder);
            }
            // sort by direction
            var sortByDirectionsBuilder = TinyImmutableArray.CreateBuilder<RestSortByDirection>();
            if (headers.TryGetValue("X-Sort-By-Direction", out values) && values.Count > 0)
            {
                ParseSortByDirections(values[0].AsSpan(), ref sortByDirectionsBuilder);
            }
            // return rest query;
            return new ValueTask<RestQuery>(new RestQuery(
                offset,
                count,
                filter,
                in sortByBuilder,
                in sortByDirectionsBuilder
            ));
        }
    }
}