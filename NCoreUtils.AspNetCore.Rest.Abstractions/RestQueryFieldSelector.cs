using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NCoreUtils.AspNetCore.Rest;

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly struct RestQueryFieldsSelector(IReadOnlyList<string>? includedFields)
{
    public static RestQueryFieldsSelector All
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => default;
    }

    public IReadOnlyList<string>? IncludedFields { get; } = includedFields;

    public bool IncludeAll
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => IncludedFields is null;
    }
}