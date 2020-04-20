using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.AspNetCore.Rest
{
    /// <summary>
    /// Defines functionality to control REST method invocation.
    /// <para>
    /// E.g. allows implementing retry logic for transactional operations.
    /// </para>
    /// </summary>
    public interface IRestMethodInvoker
    {
        /// <summary>
        /// Performes invocation of the REST method that returns value.
        /// </summary>
        /// <param name="target">Packed invocation data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <typeparam name="T">Type of the result of the operation.</typeparam>
        /// <returns>Result of the operation.</returns>
        ValueTask<T> InvokeAsync<T>(RestMethodInvocation<T> target, CancellationToken cancellationToken);

        /// <summary>
        /// Performes invocation of the REST method that does not return value.
        /// </summary>
        /// <param name="target">Packed invocation data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        ValueTask InvokeAsync(ViodRestMethodInvocation target, CancellationToken cancellationToken);
    }
}