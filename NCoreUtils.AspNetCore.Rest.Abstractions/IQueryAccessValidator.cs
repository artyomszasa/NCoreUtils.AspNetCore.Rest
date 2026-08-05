using System.Security.Claims;

namespace NCoreUtils.AspNetCore.Rest;

/// <summary>
/// Defines functionality to implement generic query level access limitation.
/// </summary>
[Obsolete("Use IQueryAccessStatusValidator instead.")]
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