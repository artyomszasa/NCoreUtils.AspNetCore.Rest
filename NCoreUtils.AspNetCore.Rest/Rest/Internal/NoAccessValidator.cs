using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.AspNetCore.Rest.Internal;

#pragma warning disable CS0618
internal class NoAccessValidator : IAccessValidator, IAccessStatusValidator
{
    public static NoAccessValidator Instance { get; } = new NoAccessValidator();

    NoAccessValidator() { }

    ValueTask<bool> IAccessValidator.ValidateAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
        => new(true);

    public ValueTask<AccessStatusValidatorResult> ValidateAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
        => new(AccessStatusValidatorResult.Succeeded);
}
#pragma warning restore CS0618