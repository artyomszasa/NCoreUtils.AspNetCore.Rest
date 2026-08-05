using System.Runtime.CompilerServices;
using System.Security.Claims;

namespace NCoreUtils.AspNetCore.Rest;

public readonly struct AccessStatusValidatorResult : IEquatable<AccessStatusValidatorResult>
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

    public override bool Equals(object? obj)
        => obj is AccessStatusValidatorResult other && Equals(other);

    public bool Equals(AccessStatusValidatorResult other)
        => Success == other.Success && StatusCode == other.StatusCode && Message == other.Message;

    public override int GetHashCode()
        => HashCode.Combine(Success, StatusCode, Message);

    public static bool operator ==(AccessStatusValidatorResult left, AccessStatusValidatorResult right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(AccessStatusValidatorResult left, AccessStatusValidatorResult right)
        => !(left == right);
}

public interface IAccessStatusValidator
{
    ValueTask<AccessStatusValidatorResult> ValidateAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
}