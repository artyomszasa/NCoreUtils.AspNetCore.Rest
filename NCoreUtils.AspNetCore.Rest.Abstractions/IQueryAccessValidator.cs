using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.AspNetCore.Rest
{
    /// <summary>
    /// Defines functionality to implement generic query level access limitation.
    /// </summary>
    public interface IQueryAccessValidator : IAccessValidator
    {
        /// <summary>
        /// Decorates specified queryable to return only accessible items.
        /// </summary>
        /// <param name="queryable">Source queryable.</param>
        /// <param name="principal">Principal information for the current context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Decorated queryable.</returns>
        ValueTask<IQueryable> FilterQueryAsync(IQueryable source, ClaimsPrincipal principal, CancellationToken cancellationToken);
    }
}