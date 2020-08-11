using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.AspNetCore.Rest
{
    public interface IBoxedInvoke
    {
        object Instance { get; }
    }

    public interface IBoxedInvoke<TArg> : IBoxedInvoke
    {
        ValueTask InvokeAsync(TArg arg, CancellationToken cancellationToken);
    }

    public interface IBoxedInvoke<TArg, TResult> : IBoxedInvoke
    {
        ValueTask<TResult> InvokeAsync(TArg arg, CancellationToken cancellationToken);
    }

    public interface IBoxedInvoke<TArg1, TArg2, TResult> : IBoxedInvoke
    {
        ValueTask<TResult> InvokeAsync(TArg1 arg1, TArg2 arg2, CancellationToken cancellationToken);
    }
}