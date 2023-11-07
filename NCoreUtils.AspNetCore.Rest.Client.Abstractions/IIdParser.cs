using System;
using System.Diagnostics.CodeAnalysis;

namespace NCoreUtils.Rest;

public interface IIdParser
{
    [return: NotNullIfNotNull(nameof(raw))]
    object? ParseId(string? raw, Type idType);
}