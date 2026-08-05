using System.Buffers;
using System.Globalization;

namespace NCoreUtils.Rest
{
    public class RestQueryAsQueryParameterSerializer : IRestQuerySerializer
    {
        private static string AscWhenNull(string? source) => string.IsNullOrEmpty(source)
            ? "asc"
            : source;

        private static string ComposeUri(
            Span<char> buffer,
            string @base,
            string? target,
            string? filter,
            string? sortBy,
            string? sortByDirection,
            IReadOnlyList<ThenBySorting>? thenBy,
            string? offset,
            string? limit)
        {
            var builder = new SpanBuilder(buffer);
            builder.Append(@base);
            var delimiter = @base.Contains('?', StringComparison.Ordinal) ? '&' : '?';
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
                if (thenBy is { Count: > 0 })
                {
                    foreach (var ord in thenBy)
                    {
                        builder.Append(',');
                        builder.Append(ord.By);
                    }
                }
                delimiter = '&';
                if (!string.IsNullOrEmpty(sortByDirection))
                {
                    builder.Append(delimiter);
                    builder.Append("sort-direction=");
                    builder.Append(AscWhenNull(sortByDirection));
                    if (thenBy is { Count: > 0 })
                    {
                        foreach (var ord in thenBy)
                        {
                            builder.Append(',');
                            builder.Append(AscWhenNull(ord.Direction));
                        }
                    }
                    delimiter = '&';
                }
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
            IReadOnlyList<ThenBySorting>? thenBy,
            string? offset,
            string? limit)
        {
            if (maxSize < 8192)
            {
                Span<char> stackBuffer = stackalloc char[maxSize];
                return ComposeUri(stackBuffer, @base, target, filter, sortBy, sortByDirection, thenBy, offset, limit);
            }
            var buffer = ArrayPool<char>.Shared.Rent(maxSize);
            try
            {
                return ComposeUri(buffer, @base, target, filter, sortBy, sortByDirection, thenBy, offset, limit);
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
            IReadOnlyList<ThenBySorting>? thenBy = default,
            int offset = 0,
            int? limit = null)
        {
            Preconditions.ThrowIfNull(request);
            var requestUri = request.RequestUri ?? throw new ArgumentException("Uri member must be initialized.", nameof(request));
            var uri = requestUri.ToString();
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
            string? sortByDirectionString = default;
            ThenBySorting[]? thenByValue = default;
            if (!string.IsNullOrEmpty(sortBy))
            {
                sortByString = Uri.EscapeDataString(sortBy);
                newUriSize += "sort-by".Length + 2 + sortByString.Length;
                sortByDirectionString = string.IsNullOrEmpty(sortByDirection) ? "asc" : Uri.EscapeDataString(sortByDirection);
                newUriSize += "sort-direction".Length + 2 + sortByDirectionString.Length;
                if (thenBy is { Count: > 0 })
                {
                    thenByValue = new ThenBySorting[thenBy.Count];
                    newUriSize += thenBy.Count * 2;
                    var i = 0;
                    foreach (var ord in thenBy)
                    {
                        var thenByString = Uri.EscapeDataString(ord.By ?? string.Empty);
                        newUriSize += thenByString.Length;
                        var thenByDirectionString = string.IsNullOrEmpty(ord.Direction) ? "asc" : Uri.EscapeDataString(ord.Direction);
                        newUriSize += thenByDirectionString.Length;
                        thenByValue[i++] = new(thenByString, thenByDirectionString);
                    }
                }
            }
            string? offsetString = offset == 0 ? default : offset.ToString(CultureInfo.InvariantCulture);
            if (!string.IsNullOrEmpty(offsetString))
            {
                newUriSize += "offset".Length + 2 + offsetString!.Length;
            }
            string? limitString = limit.HasValue && limit.Value != -1 ? limit.Value.ToString(CultureInfo.InvariantCulture) : default;
            if (!string.IsNullOrEmpty(limitString))
            {
                newUriSize += "count".Length + 2 + limitString!.Length;
            }
            request.RequestUri = new Uri(
                ComposeUri(newUriSize, uri, targetString, filterString, sortByString, sortByDirectionString, thenByValue, offsetString, limitString),
                uriKind: uri.StartsWith('/')
                    ? UriKind.Relative
                    : UriKind.Absolute
            );
        }
    }
}