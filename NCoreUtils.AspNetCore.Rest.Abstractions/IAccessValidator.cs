using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.AspNetCore.Rest
{
    /// <summary>
    /// Defines functionality to implement generic access validation.
    /// </summary>
    public interface IAccessValidator
    {
        /// <summary>
        /// Returns whether operation is accessible with respect to the specified parameters.
        /// </summary>
        /// <param name="principal">Principal information for the current context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><c>true</c> if operation can be performed, otherwise <c>false</c></returns>
        ValueTask<bool> ValidateAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
    }
}