namespace NCoreUtils.AspNetCore.Rest;

public class DefaultRestMethodInvoker : IRestMethodInvoker
{
    public static DefaultRestMethodInvoker Instance { get; } = new();

    protected virtual async ValueTask<T> InvokeTransactedAsync<T>(RestMethodInvocation<T> target, IRestTransactedMethod txMethod, CancellationToken cancellationToken)
    {
        Preconditions.ThrowIfNull(txMethod);
        Preconditions.ThrowIfNull(target);
        using var tx = await txMethod.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var result = await target.InvokeAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }

    protected virtual async ValueTask InvokeTransactedAsync(ViodRestMethodInvocation target, IRestTransactedMethod txMethod, CancellationToken cancellationToken)
    {
        Preconditions.ThrowIfNull(txMethod);
        Preconditions.ThrowIfNull(target);
        using var tx = await txMethod.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await target.InvokeAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public virtual ValueTask<T> InvokeAsync<T>(RestMethodInvocation<T> target, CancellationToken cancellationToken)
    {
        Preconditions.ThrowIfNull(target);
        if (target.Instance is IRestTransactedMethod txMethod)
        {
            return InvokeTransactedAsync(target, txMethod, cancellationToken);
        }
        return target.InvokeAsync(cancellationToken);
    }

    public ValueTask InvokeAsync(ViodRestMethodInvocation target, CancellationToken cancellationToken)
    {
        Preconditions.ThrowIfNull(target);
        if (target.Instance is IRestTransactedMethod txMethod)
        {
            return InvokeTransactedAsync(target, txMethod, cancellationToken);
        }
        return target.InvokeAsync(cancellationToken);
    }

    public IAsyncEnumerable<T> InvokeAsync<T>(RestMethodEnumerableInvocation<T> target, CancellationToken cancellationToken)
    {
        Preconditions.ThrowIfNull(target);
        return target.InvokeAsync(cancellationToken);
    }
}