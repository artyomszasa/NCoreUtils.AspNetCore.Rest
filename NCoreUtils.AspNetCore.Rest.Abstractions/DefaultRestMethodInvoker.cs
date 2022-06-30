using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.AspNetCore.Rest
{
    public class DefaultRestMethodInvoker : IRestMethodInvoker
    {
        public static DefaultRestMethodInvoker Instance { get; } = new DefaultRestMethodInvoker();

        protected virtual async ValueTask<T> InvokeTransactedAsync<T>(RestMethodInvocation<T> target, IRestTransactedMethod txMethod, CancellationToken cancellationToken)
        {
            using var tx = await txMethod.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            var result = await target.InvokeAsync(cancellationToken).ConfigureAwait(false);
            tx.Commit();
            return result;
        }

        protected virtual async ValueTask InvokeTransactedAsync(ViodRestMethodInvocation target, IRestTransactedMethod txMethod, CancellationToken cancellationToken)
        {
            using var tx = await txMethod.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await target.InvokeAsync(cancellationToken).ConfigureAwait(false);
            tx.Commit();
        }

        public virtual ValueTask<T> InvokeAsync<T>(RestMethodInvocation<T> target, CancellationToken cancellationToken)
        {
            if (target.Instance is IRestTransactedMethod txMethod)
            {
                return InvokeTransactedAsync(target, txMethod, cancellationToken);
            }
            return target.InvokeAsync(cancellationToken);
        }

        public ValueTask InvokeAsync(ViodRestMethodInvocation target, CancellationToken cancellationToken)
        {
            if (target.Instance is IRestTransactedMethod txMethod)
            {
                return InvokeTransactedAsync(target, txMethod, cancellationToken);
            }
            return target.InvokeAsync(cancellationToken);
        }

        public IAsyncEnumerable<T> InvokeAsync<T>(RestMethodEnumerableInvocation<T> target, CancellationToken cancellationToken)
            => target.InvokeAsync(cancellationToken);
    }
}