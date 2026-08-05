using System.Runtime.CompilerServices;

namespace NCoreUtils.AspNetCore.Rest;

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly struct RestQueryFieldsSelector(IReadOnlyList<string>? includedFields)
    : IEquatable<RestQueryFieldsSelector>
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

    #region equality

    public bool Equals(RestQueryFieldsSelector other)
    {
        if (IncludedFields is IReadOnlyList<string> thisIncludedFields)
        {
            return other.IncludedFields is IReadOnlyList<string> otherIncludedFields
                && thisIncludedFields.SequenceEqual(otherIncludedFields, StringComparer.Ordinal);
        }
        return other.IncludedFields is null;
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is RestQueryFieldsSelector other && Equals(other);

    public override int GetHashCode()
    {
        if (IncludedFields is not IReadOnlyList<string> includedFields)
        {
            return default;
        }
        var builder = new HashCode();
        builder.Add(includedFields.Count);
        foreach (var field in includedFields)
        {
            builder.Add(field, StringComparer.Ordinal);
        }
        return builder.ToHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(RestQueryFieldsSelector left, RestQueryFieldsSelector right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(RestQueryFieldsSelector left, RestQueryFieldsSelector right) => !(left == right);

    #endregion
}