using System.Runtime.ExceptionServices;

namespace NCoreUtils.AspNetCore.Rest;

public interface IRestErrorAccessor
{
#pragma warning disable CA1716 // Identifiers should not match keywords
    ExceptionDispatchInfo? Error { get; }
#pragma warning restore CA1716 // Identifiers should not match keywords
}