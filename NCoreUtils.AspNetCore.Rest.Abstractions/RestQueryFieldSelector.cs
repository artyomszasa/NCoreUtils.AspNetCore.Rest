using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NCoreUtils.AspNetCore.Rest
{
    public struct RestQueryFieldsSelector
    {
        public static RestQueryFieldsSelector All
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => default;
        }

        public IReadOnlyList<string>? IncludedFields { get; }

        public bool IncludeAll
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IncludedFields is null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RestQueryFieldsSelector(IReadOnlyList<string>? includedFields)
            => IncludedFields = includedFields;
    }
}