using System;
using System.Runtime.ExceptionServices;

namespace NCoreUtils.AspNetCore.Rest
{
    public interface IRestErrorAccessor
    {
        ExceptionDispatchInfo? Error { get; }
    }
}