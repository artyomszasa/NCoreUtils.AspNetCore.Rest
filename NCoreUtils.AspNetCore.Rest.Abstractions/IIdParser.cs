using System;

namespace NCoreUtils.AspNetCore.Rest;

public interface IIdParser
{
    object? ParseId(string? raw, Type type);
}