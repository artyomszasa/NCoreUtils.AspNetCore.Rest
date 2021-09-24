using System.Net.Http;

namespace NCoreUtils.Rest
{
    public interface IRestQuerySerializer
    {
        void Apply(
            HttpRequestMessage request,
            string? target = null,
            string? filter = null,
            string? sortBy = null,
            string? sortByDirection = null,
            int offset = 0,
            int? limit = null);
    }
}