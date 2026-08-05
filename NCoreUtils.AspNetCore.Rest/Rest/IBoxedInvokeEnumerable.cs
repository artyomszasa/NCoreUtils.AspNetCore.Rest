namespace NCoreUtils.AspNetCore.Rest
{
    [Obsolete("Not used and will be pruned")]
    public interface IBoxedInvokeEnumerable<TArg1, TArg2, TResult> : IBoxedInvoke
    {
        IAsyncEnumerable<TResult> InvokeAsync(TArg1 arg1, TArg2 arg2, CancellationToken cancellationToken);
    }
}