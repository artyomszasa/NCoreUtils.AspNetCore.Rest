using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.AspNetCore.Rest
{
    /// <summary>
    /// Defines functionality to implement configurable output of the specified type.
    /// </summary>
    /// <typeparam name="T">Type of the underlying output target.</typeparam>
    public interface IConfigurableOutput<T>
    {
        /// <summary>
        /// Initializes output with the specified output information
        /// </summary>
        /// <param name="info">Optional output information.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Confugured output.</returns>
        ValueTask<T> InitializeAsync(OutputInfo info, CancellationToken cancellationToken);
    }
}