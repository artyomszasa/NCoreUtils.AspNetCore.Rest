using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using NCoreUtils.Collections;

namespace NCoreUtils.AspNetCore.Rest
{
    public sealed class RestQuery : IDisposable
    {
        public const int DefaultCount = 10000;

        private ArraySegment<string>? _fields;

        private ArraySegment<string>? _sortBy;

        private ArraySegment<RestSortByDirection>? _sortByDirections;

        public int? Offset { get; }

        public int? Count { get; }

        public string? Filter { get; }

        /// <summary>
        /// If not <c>null</c> contains names of the object properties that should be included in the response,
        /// otherwise all fields should be included. When not <c>null</c> the property names are sorted using culture
        /// invariant comparison.
        /// </summary>
        public ArraySegment<string>? Fields
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _fields;
        }

        public ArraySegment<string>? SortBy
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _sortBy;
        }

        public ArraySegment<RestSortByDirection>? SortByDirections
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _sortByDirections;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal RestQuery(
            int? offset,
            int? count,
            string? filter,
            ArraySegment<string>? fields,
            ArraySegment<string>? sortBy,
            ArraySegment<RestSortByDirection>? sortByDirections)
        {
            Offset = offset;
            Count = count;
            Filter = filter;
            _fields = fields;
            _sortBy = sortBy;
            _sortByDirections = sortByDirections;
            if (_fields.HasValue)
            {
                Array.Sort(_fields.Value.Array!, _fields.Value.Offset, _fields.Value.Count, StringComparer.InvariantCulture);
            }
        }

        void IDisposable.Dispose()
        {
            if (_fields.HasValue)
            {
                var fields = _fields.Value;
                _fields = default;
                ArrayPool<string>.Shared.Return(fields.Array!);
            }
            if (_sortBy.HasValue)
            {
                var sortBy = _sortBy.Value;
                _sortBy = default;
                ArrayPool<string>.Shared.Return(sortBy.Array!);
            }
            if (_sortByDirections.HasValue)
            {
                var sortByDirections = _sortByDirections.Value;
                _sortByDirections = default;
                ArrayPool<RestSortByDirection>.Shared.Return(sortByDirections.Array!);
            }
        }

        internal RestQuery Override(RestQuery other)
        {
            var offset = other.Offset ?? Offset;
            var count = other.Count ?? Count;
            var filter = other.Filter ?? Filter;
            ArraySegment<string>? fields;
            if (other._fields.HasValue)
            {
                fields = other._fields;
                other._fields = default;
            }
            else if (_fields.HasValue)
            {
                fields = _fields;
                _fields = default;
            }
            else
            {
                fields = default;
            }
            ArraySegment<string>? sortBy;
            if (other._sortBy.HasValue)
            {
                sortBy = other._sortBy;
                other._sortBy = default;
            }
            else if (_sortBy.HasValue)
            {
                sortBy = _sortBy;
                _sortBy = default;
            }
            else
            {
                sortBy = default;
            }
            ArraySegment<RestSortByDirection>? sortByDirections;
            if (other._sortByDirections.HasValue)
            {
                sortByDirections = other._sortByDirections;
                other._sortByDirections = default;
            }
            else if (_sortByDirections.HasValue)
            {
                sortByDirections = _sortByDirections;
                _sortByDirections = default;
            }
            else
            {
                sortByDirections = default;
            }
            return new RestQuery(offset, count, filter, fields, sortBy, sortByDirections);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetOffset()
            => Offset ?? 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetCount()
            => Count ?? DefaultCount;
    }
}