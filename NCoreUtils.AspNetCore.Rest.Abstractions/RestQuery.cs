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

        private string[]? _fields;

        private string[]? _sortBy;

        private RestSortByDirection[]? _sortByDirections;

        public int? Offset { get; }

        public int? Count { get; }

        public string? Filter { get; }

        /// <summary>
        /// If not <c>null</c> contains names of the object properties that should be included in the response,
        /// otherwise all fields should be included. When not <c>null</c> the property names are sorted using culture
        /// invariant comparison.
        /// </summary>
        public IReadOnlyList<string>? Fields
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _fields;
        }

        public IReadOnlyList<string>? SortBy
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _sortBy;
        }

        public IReadOnlyList<RestSortByDirection>? SortByDirections
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _sortByDirections;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal RestQuery(
            int? offset,
            int? count,
            string? filter,
            string[]? fields,
            string[]? sortBy,
            RestSortByDirection[]? sortByDirections)
        {
            Offset = offset;
            Count = count;
            Filter = filter;
            _fields = fields;
            _sortBy = sortBy;
            _sortByDirections = sortByDirections;
            if (!(_fields is null))
            {
                Array.Sort(_fields, StringComparer.InvariantCulture);
            }
        }

        void IDisposable.Dispose()
        {
            var fields = Interlocked.Exchange(ref _fields, default);
            if (!(fields is null))
            {
                ArrayPool<string>.Shared.Return(fields);
            }
            var sortBy = Interlocked.Exchange(ref _sortBy, default);
            if (!(sortBy is null))
            {
                ArrayPool<string>.Shared.Return(sortBy);
            }
            var sortByDirections = Interlocked.Exchange(ref _sortByDirections, default);
            if (!(sortByDirections is null))
            {
                ArrayPool<RestSortByDirection>.Shared.Return(sortByDirections);
            }
        }

        internal RestQuery Override(RestQuery other)
        {
            var offset = other.Offset ?? Offset;
            var count = other.Count ?? Count;
            var filter = other.Filter ?? Filter;
            string[]? fields;
            if (!(other._fields is null))
            {
                fields = Interlocked.Exchange(ref other._fields, default);
            }
            else if (!(_fields is null))
            {
                fields = Interlocked.Exchange(ref _fields, default);
            }
            else
            {
                fields = default;
            }
            string[]? sortBy;
            if (!(other._sortBy is null))
            {
                sortBy = Interlocked.Exchange(ref other._sortBy, default);
            }
            else if (!(_sortBy is null))
            {
                sortBy = Interlocked.Exchange(ref _sortBy, default);
            }
            else
            {
                sortBy = default;
            }
            RestSortByDirection[]? sortByDirections;
            if (!(other._sortByDirections is null))
            {
                sortByDirections = Interlocked.Exchange(ref other._sortByDirections, default);
            }
            else if (!(_sortBy is null))
            {
                sortByDirections = Interlocked.Exchange(ref _sortByDirections, default);
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