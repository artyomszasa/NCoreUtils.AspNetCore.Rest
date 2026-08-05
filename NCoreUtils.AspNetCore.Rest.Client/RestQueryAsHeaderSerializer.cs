using System.Globalization;

namespace NCoreUtils.Rest
{
    public class RestQueryAsHeaderSerializer : IRestQuerySerializer
    {
        public static RestQueryAsHeaderSerializer Instance { get; } = new RestQueryAsHeaderSerializer();

        private static string AscWhenNull(string? source) => string.IsNullOrEmpty(source)
            ? "asc"
            : source;

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
            if (!string.IsNullOrEmpty(filter))
            {
                request.Headers.Add("X-Filter", Uri.EscapeDataString(filter));
            }
            if (!string.IsNullOrEmpty(sortBy))
            {
                if (thenBy is { Count: > 0 })
                {
                    var sortByValue = string.Join(',', [sortBy, ..thenBy.Select(ord => ord.By)]);
                    var sortByDirectionValue = string.Join(',', [AscWhenNull(sortByDirection), ..thenBy.Select(ord => AscWhenNull(ord.Direction))]);
                    request.Headers.Add("X-Sort-By", Uri.EscapeDataString(sortByValue));
                    request.Headers.Add("X-Sort-By-Direction", sortByDirectionValue);
                }
                else
                {
                    request.Headers.Add("X-Sort-By", Uri.EscapeDataString(sortBy));
                    request.Headers.Add("X-Sort-By-Direction", sortByDirection);
                }
            }
            request.Headers.Add("X-Offset", offset.ToString(CultureInfo.InvariantCulture));
            if (limit.HasValue)
            {
                request.Headers.Add("X-Count", limit.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (!string.IsNullOrEmpty(target))
            {
                request.Headers.Add("X-Type", target);
            }
        }
    }
}