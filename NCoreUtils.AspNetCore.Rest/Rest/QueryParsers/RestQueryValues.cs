using NCoreUtils.Collections;

namespace NCoreUtils.AspNetCore.Rest.QueryParsers
{
    public class RestQueryValues
    {
        private readonly TinyImmutableArray<string>? _sortBy;

        private readonly TinyImmutableArray<RestSortByDirection>? _sortByDirection;

        private readonly RestQueryFieldsSelector _fields;

        public int? Offset { get; }

        public int? Count { get; }

        public string? Filter { get; }

        public ref readonly RestQueryFieldsSelector Fields
            => ref _fields;

        public ref readonly TinyImmutableArray<string>? SortBy
            => ref _sortBy;

        public ref readonly TinyImmutableArray<RestSortByDirection>? SortByDirection
            => ref _sortByDirection;

        public RestQueryValues(
            int? offset,
            int? count,
            string? filter,
            in RestQueryFieldsSelector fields,
            in TinyImmutableArray<string>? sortBy,
            in TinyImmutableArray<RestSortByDirection>? sortByDirection)
        {
            Offset = offset;
            Count = count;
            Filter = filter;
            _fields = fields;
            _sortBy = sortBy;
            _sortByDirection = sortByDirection;
        }

        public RestQueryValues Override(RestQueryValues other)
        {
            if (other is null)
            {
                throw new System.ArgumentNullException(nameof(other));
            }
            var sortBy = other.SortBy ?? SortBy;
            var sortByDirection = other.SortByDirection ?? SortByDirection;
            var fields = other.Fields.IncludeAll ? Fields : other.Fields;
            return new RestQueryValues(
                other.Offset ?? Offset,
                other.Count ?? Count,
                other.Filter ?? Filter,
                in fields,
                in sortBy,
                in sortByDirection
            );
        }
    }
}