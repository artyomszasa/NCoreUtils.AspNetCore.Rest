using System;
using System.Globalization;
using System.Net.Http;

namespace NCoreUtils.Rest
{
    public class RestQueryAsHeaderSerializer : IRestQuerySerializer
    {
        public static RestQueryAsHeaderSerializer Instance { get; } = new RestQueryAsHeaderSerializer();

        public void Apply(
            HttpRequestMessage request,
            string? target = null,
            string? filter = null,
            string? sortBy = null,
            string? sortByDirection = null,
            int offset = 0,
            int? limit = null)
        {
            if (!string.IsNullOrEmpty(filter))
            {
                request.Headers.Add("X-Filter", Uri.EscapeDataString(filter));
            }
            if (!string.IsNullOrEmpty(sortBy))
            {
                request.Headers.Add("X-Sort-By", Uri.EscapeDataString(sortBy));
                request.Headers.Add("X-Sort-By-Direction", sortByDirection);
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