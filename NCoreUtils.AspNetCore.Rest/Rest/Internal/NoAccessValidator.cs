using System.Security.Claims;

namespace NCoreUtils.AspNetCore.Rest.Internal;

internal sealed class NoAccessValidator : IAccessStatusValidator
{
    public static NoAccessValidator Singleton { get; } = new NoAccessValidator();

    NoAccessValidator() { }

    public ValueTask<AccessStatusValidatorResult> ValidateAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
        => new(AccessStatusValidatorResult.Succeeded);
}