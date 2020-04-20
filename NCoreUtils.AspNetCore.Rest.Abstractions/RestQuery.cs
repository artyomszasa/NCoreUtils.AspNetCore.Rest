using NCoreUtils.Collections;

namespace NCoreUtils.AspNetCore.Rest
{
    public class RestQuery
    {
        public int Offset { get; }

        public int Count { get; }

        public string? Filter { get; }

        public TinyImmutableArray<string> SortBy { get; }

        public TinyImmutableArray<RestSortByDirection> SortByDirection { get; }

        public RestQuery(
            int offset,
            int count,
            string? filter,
            in TinyImmutableArray<string>.Builder sortBy,
            in TinyImmutableArray<RestSortByDirection>.Builder sortByDirection)
        {
            Offset = offset;
            Count = count;
            Filter = filter;
            SortBy = sortBy.Build();
            SortByDirection = sortByDirection.Build();
        }
    }
}