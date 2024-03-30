using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace NCoreUtils.AspNetCore.Rest;

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
[method: DebuggerStepThrough]
public readonly struct OutputInfo(long? length, string? contentType) : IEquatable<OutputInfo>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public static bool operator==(OutputInfo a, OutputInfo b) => a.Equals(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public static bool operator!=(OutputInfo a, OutputInfo b) => !a.Equals(b);

    public long? Length { get; } = length;

    public string? ContentType { get; } = contentType;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public bool Equals(OutputInfo other)
        => Length == other.Length
            && ContentType == other.ContentType;

    [DebuggerStepThrough]
    public override bool Equals(object? obj)
        => obj is OutputInfo other && Equals(other);

    [DebuggerStepThrough]
    public override int GetHashCode()
        => HashCode.Combine(Length, ContentType);
}