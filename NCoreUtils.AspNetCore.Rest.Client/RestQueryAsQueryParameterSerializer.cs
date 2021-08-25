using System;
using System.Buffers;
using System.Globalization;
using System.Net.Http;

namespace NCoreUtils.Rest
{
    public class RestQueryAsQueryParameterSerializer : IRestQuerySerializer
    {
        private static string ComposeUri(
            Span<char> buffer,
            string @base,
            string? target,
            string? filter,
            string? sortBy,
            string? sortByDirection,
            string? offset,
            string? limit)
        {
            var builder = new SpanBuilder(buffer);
            builder.Append(@base);
            #if NESTANDARD2_1
            var delimiter = @base.Contains('?') ? '&' : '?';
            #else
            var delimiter = @base.Contains("?") ? '&' : '?';
            #endif
            if (!string.IsNullOrEmpty(target))
            {
                builder.Append(delimiter);
                builder.Append("target=");
                builder.Append(target!);
                delimiter = '&';
            }
            if (!string.IsNullOrEmpty(filter))
            {
                builder.Append(delimiter);
                builder.Append("filter=");
                builder.Append(filter!);
                delimiter = '&';
            }
            if (!string.IsNullOrEmpty(sortBy))
            {
                builder.Append(delimiter);
                builder.Append("sort-by=");
                builder.Append(sortBy!);
                delimiter = '&';
            }
            if (!string.IsNullOrEmpty(sortByDirection))
            {
                builder.Append(delimiter);
                builder.Append("sort-direction=");
                builder.Append(sortByDirection!);
                delimiter = '&';
            }
            if (!string.IsNullOrEmpty(offset))
            {
                builder.Append(delimiter);
                builder.Append("offset=");
                builder.Append(offset!);
                delimiter = '&';
            }
            if (!string.IsNullOrEmpty(limit))
            {
                builder.Append(delimiter);
                builder.Append("count=");
                builder.Append(limit!);
                delimiter = '&';
            }
            return builder.ToString();
        }

        private static string ComposeUri(
            int maxSize,
            string @base,
            string? target,
            string? filter,
            string? sortBy,
            string? sortByDirection,
            string? offset,
            string? limit)
        {
            if (maxSize < 8192)
            {
                Span<char> stackBuffer = stackalloc char[maxSize];
                return ComposeUri(stackBuffer, @base, target, filter, sortBy, sortByDirection, offset, limit);
            }
            var buffer = ArrayPool<char>.Shared.Rent(maxSize);
            try
            {
                return ComposeUri(buffer, @base, target, filter, sortBy, sortByDirection, offset, limit);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(buffer);
            }
        }

        public void Apply(
            HttpRequestMessage request,
            string? target = null,
            string? filter = null,
            string? sortBy = null,
            string? sortByDirection = null,
            int offset = 0,
            int? limit = null)
        {
            var uri = request.RequestUri.AbsoluteUri;
            var newUriSize = uri.Length;
            string? targetString = default;
            if (!string.IsNullOrEmpty(target))
            {
                targetString = Uri.EscapeDataString(target);
                newUriSize += "target".Length + 2 + targetString.Length;
            }
            string? filterString = default;
            if (!string.IsNullOrEmpty(filter))
            {
                filterString = Uri.EscapeDataString(filter);
                newUriSize += "filter".Length + 2 + filterString.Length;
            }
            string? sortByString = default;
            if (!string.IsNullOrEmpty(sortBy))
            {
                sortByString = Uri.EscapeDataString(sortByString);
                newUriSize += "sort-by".Length + 2 + sortByString.Length;
            }
            string? sortByDirectionString = default;
            if (!string.IsNullOrEmpty(sortByDirection))
            {
                sortByDirectionString = Uri.EscapeDataString(sortByDirection);
                newUriSize += "sort-direction".Length + 2 + sortByDirectionString.Length;
            }
            string? offsetString = offset == 0 ? default : offset.ToString(CultureInfo.InvariantCulture);
            if (!string.IsNullOrEmpty(offsetString))
            {
                newUriSize += "offset".Length + 2 + offsetString!.Length;
            }
            string? limitString = limit.HasValue ? limit.Value.ToString() : default;
            if (!string.IsNullOrEmpty(limitString))
            {
                newUriSize += "count".Length + 2 + limitString!.Length;
            }
            request.RequestUri = new Uri(ComposeUri(newUriSize, uri, targetString, filterString, sortByString, sortByDirectionString, offsetString, limitString));
        }
    }
}