using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace NCoreUtils;

internal static class Check
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ThrowIfNull<T>([NotNull] this T? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = default)
        where T : class
    {
#if !NET6_0_OR_GREATER
        if (argument is null)
        {
            throw new ArgumentNullException(paramName);
        }
        return argument!;
#else
        ArgumentNullException.ThrowIfNull(argument, paramName);
        return argument;
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ThrowIfNullOrWhiteSpace([NotNull] this string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = default)
    {
#if !NET8_0_OR_GREATER
        if (string.IsNullOrWhiteSpace(argument))
        {
            throw new ArgumentException($"'{paramName}' cannot be null or whitespace.", paramName);
        }
#pragma warning disable CS8777
        return argument!;
#pragma warning restore CS8777
#else
        ArgumentException.ThrowIfNullOrWhiteSpace(argument, paramName);
        return argument;
#endif
    }
}