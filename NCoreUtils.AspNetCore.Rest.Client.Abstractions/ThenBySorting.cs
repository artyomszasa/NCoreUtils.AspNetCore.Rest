using System;

namespace NCoreUtils.Rest;

public readonly struct ThenBySorting(string by, string direction) : IEquatable<ThenBySorting>
{
    public string By { get; } = by;

    public string Direction { get; } = direction;

    public bool Equals(ThenBySorting other)
        => string.Equals(By, other.By, StringComparison.Ordinal) &&
           string.Equals(Direction, other.Direction, StringComparison.Ordinal);

    public override bool Equals(object? obj)
        => obj is ThenBySorting other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(By, Direction);

    public static bool operator ==(ThenBySorting left, ThenBySorting right) => left.Equals(right);
    public static bool operator !=(ThenBySorting left, ThenBySorting right) => !left.Equals(right);
}