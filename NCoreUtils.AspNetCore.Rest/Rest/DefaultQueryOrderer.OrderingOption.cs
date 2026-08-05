using System.Runtime.CompilerServices;

namespace NCoreUtils.AspNetCore.Rest;

public partial class DefaultQueryOrderer
{
    /// Represents ordering option.
    /// <summary>
    /// Initializes new instance from the specified parameters.
    /// </summary>
    /// <param name="by">Ordering criteria.</param>
    /// <param name="isDescending">Ordering direction.</param>
    protected readonly struct OrderingOption(string by, bool isDescending) : IEquatable<OrderingOption>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator==(OrderingOption a, OrderingOption b) => a.Equals(b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator!=(OrderingOption a, OrderingOption b) => !a.Equals(b);

        /// Ordering criteria.
        public string By { get; } = by;

        /// Ordering direction.
        public bool IsDescending { get; } = isDescending;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(OrderingOption other)
            => By == other.By && IsDescending == other.IsDescending;

        public override bool Equals(object? obj)
            => obj is OrderingOption other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(By, IsDescending);
    }
}