using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Data;

namespace NCoreUtils.AspNetCore.Rest
{
    /// <summary>
    /// Defines functionality to customize transactions used within trasnactional REST methods.
    /// </summary>
    public interface IRestTransactedMethod
    {
        /// <summary>
        /// Initiates transaction required to perform transactional operation.
        /// </summary>
        /// <returns>Transaction to use.</returns>
        ValueTask<IDataTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
    }
}