using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.AspNetCore.Rest;

public readonly struct AccessStatusValidatorResult
{
    public static readonly AccessStatusValidatorResult Succeeded = new(true, default, default);

    internal static readonly AccessStatusValidatorResult FallbackFailure
        = new(false, default, default);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AccessStatusValidatorResult Failed(int statusCode, string? message = default)
        => new(false, statusCode, message);

    public bool Success { get; }

    public int? StatusCode { get; }

    public string? Message { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private AccessStatusValidatorResult(bool success, int? statusCode, string? message)
    {
        Success = success;
        StatusCode = statusCode;
        Message = message;
    }
}

public interface IAccessStatusValidator
{
    ValueTask<AccessStatusValidatorResult> ValidateAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
}