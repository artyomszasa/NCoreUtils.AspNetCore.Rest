using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NCoreUtils.AspNetCore.Rest.Internal;

#pragma warning disable CS0618
public abstract class QueryValidatorAdapter : IQueryAccessStatusValidator, IQueryAccessValidator
{
    private sealed class AdaptedQueryValidator : QueryValidatorAdapter
    {
        private IQueryAccessValidator Source { get; }

        public AdaptedQueryValidator(IQueryAccessValidator source)
            => Source = source;

        public override ValueTask<bool> ValidateAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
            => Source.ValidateAsync(principal, cancellationToken);

        public override ValueTask<IQueryable> FilterQueryAsync(IQueryable source, ClaimsPrincipal principal, CancellationToken cancellationToken)
            => Source.FilterQueryAsync(source, principal, cancellationToken);
    }

    public static IQueryAccessStatusValidator Adapt(IQueryAccessValidator source) => source switch
    {
        null => throw new ArgumentNullException(nameof(source)),
        IQueryAccessStatusValidator validator => validator,
        _ => new AdaptedQueryValidator(source)
    };

    public abstract ValueTask<IQueryable> FilterQueryAsync(IQueryable source, ClaimsPrincipal principal, CancellationToken cancellationToken);

    public abstract ValueTask<bool> ValidateAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);

    async ValueTask<AccessStatusValidatorResult> IAccessStatusValidator.ValidateAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var success = await ValidateAsync(principal, cancellationToken).ConfigureAwait(false);
        return success ? AccessStatusValidatorResult.Succeeded : AccessStatusValidatorResult.Failed(StatusCodes.Status401Unauthorized);
    }
}
#pragma warning restore CS0618