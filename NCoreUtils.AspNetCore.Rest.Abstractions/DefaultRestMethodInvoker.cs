using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.AspNetCore.Rest;

public class DefaultRestMethodInvoker : IRestMethodInvoker
{
    public static DefaultRestMethodInvoker Instance { get; } = new();

    protected virtual async ValueTask<T> InvokeTransactedAsync<T>(RestMethodInvocation<T> target, IRestTransactedMethod txMethod, CancellationToken cancellationToken)
    {
        using var tx = await txMethod.ThrowIfNull().BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var result = await target.ThrowIfNull().InvokeAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }

    protected virtual async ValueTask InvokeTransactedAsync(ViodRestMethodInvocation target, IRestTransactedMethod txMethod, CancellationToken cancellationToken)
    {
        using var tx = await txMethod.ThrowIfNull().BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await target.ThrowIfNull().InvokeAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public virtual ValueTask<T> InvokeAsync<T>(RestMethodInvocation<T> target, CancellationToken cancellationToken)
    {
        if (target.ThrowIfNull().Instance is IRestTransactedMethod txMethod)
        {
            return InvokeTransactedAsync(target, txMethod, cancellationToken);
        }
        return target.InvokeAsync(cancellationToken);
    }

    public ValueTask InvokeAsync(ViodRestMethodInvocation target, CancellationToken cancellationToken)
    {
        if (target.ThrowIfNull().Instance is IRestTransactedMethod txMethod)
        {
            return InvokeTransactedAsync(target, txMethod, cancellationToken);
        }
        return target.InvokeAsync(cancellationToken);
    }

    public IAsyncEnumerable<T> InvokeAsync<T>(RestMethodEnumerableInvocation<T> target, CancellationToken cancellationToken)
        => target.ThrowIfNull().InvokeAsync(cancellationToken);
}