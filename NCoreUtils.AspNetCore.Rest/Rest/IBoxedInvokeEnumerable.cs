using System;
using System.Collections.Generic;
using System.Threading;

namespace NCoreUtils.AspNetCore.Rest
{
    [Obsolete]
    public interface IBoxedInvokeEnumerable<TArg1, TArg2, TResult> : IBoxedInvoke
    {
        IAsyncEnumerable<TResult> InvokeAsync(TArg1 arg1, TArg2 arg2, CancellationToken cancellationToken);
    }
}