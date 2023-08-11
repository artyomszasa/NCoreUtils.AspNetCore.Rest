using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NCoreUtils.AspNetCore.Rest.Internal;

#pragma warning disable CS0618
public abstract class ValidatorAdapter : IAccessStatusValidator, IAccessValidator
{
    private sealed class AdaptedValidator : ValidatorAdapter
    {
        private IAccessValidator Source { get; }

        public AdaptedValidator(IAccessValidator source)
            => Source = source;

        public override ValueTask<bool> ValidateAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
            => Source.ValidateAsync(principal, cancellationToken);
    }

    public static IAccessStatusValidator Adapt(IAccessValidator source) => source switch
    {
        null => throw new ArgumentNullException(nameof(source)),
        IAccessStatusValidator validator => validator,
        _ => new AdaptedValidator(source)
    };

    public abstract ValueTask<bool> ValidateAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);

    async ValueTask<AccessStatusValidatorResult> IAccessStatusValidator.ValidateAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var success = await ValidateAsync(principal, cancellationToken).ConfigureAwait(false);
        return success ? AccessStatusValidatorResult.Succeeded : AccessStatusValidatorResult.Failed(StatusCodes.Status401Unauthorized);
    }
}
#pragma warning restore CS0618