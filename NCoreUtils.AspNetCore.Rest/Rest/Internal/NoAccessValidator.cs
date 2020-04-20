using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.AspNetCore.Rest.Internal
{
    internal class NoAccessValidator : IAccessValidator
    {
        public static NoAccessValidator Instance { get; } = new NoAccessValidator();

        NoAccessValidator() { }

        public ValueTask<bool> ValidateAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
            => new ValueTask<bool>(true);
    }
}