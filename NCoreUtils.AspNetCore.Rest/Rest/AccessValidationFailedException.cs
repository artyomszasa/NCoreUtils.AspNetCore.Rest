using System;
using System.Runtime.Serialization;
using Microsoft.AspNetCore.Http;

namespace NCoreUtils.AspNetCore.Rest;

[Serializable]
public class AccessValidationFailedException : InvalidOperationException, IStatusCodeResponse
{
    public const string DefaultMessage = "REST access validation has failed.";

    public const int DefaultStatusCode = StatusCodes.Status401Unauthorized;

    public int StatusCode { get; }

    protected AccessValidationFailedException(SerializationInfo info, StreamingContext context)
        : base(info, context)
        => StatusCode = info.GetInt32(nameof(StatusCode));

    public AccessValidationFailedException() : this(DefaultStatusCode) { /* noop */ }

    public AccessValidationFailedException(int statusCode, string? message = default)
        : base(message ?? DefaultMessage)
        => StatusCode = statusCode;

    public AccessValidationFailedException(int? statusCode, string? message = default)
        : this(statusCode ?? DefaultStatusCode, message ?? DefaultMessage)
    { /* noop */ }

    public AccessValidationFailedException(string message)
        : this(DefaultStatusCode, message)
    { /* noop */ }

    public AccessValidationFailedException(int statusCode, Exception innerException)
        : this(statusCode, DefaultMessage, innerException)
    { /* noop */ }

    public AccessValidationFailedException(int? statusCode, Exception innerException)
        : this(statusCode ?? DefaultStatusCode, DefaultMessage, innerException)
    { /* noop */ }

    public AccessValidationFailedException(Exception innerException)
        : this(DefaultStatusCode, innerException)
    { /* noop */ }

    public AccessValidationFailedException(int statusCode, string? message, Exception innerException)
        : base(message ?? DefaultMessage, innerException)
        => StatusCode = statusCode;

    public AccessValidationFailedException(int? statusCode, string? message, Exception innerException)
        : this(statusCode ?? DefaultStatusCode, message ?? DefaultMessage, innerException)
    { /* noop */ }

    public AccessValidationFailedException(string? message, Exception innerException)
        : this(DefaultStatusCode, message ?? DefaultMessage, innerException)
    { /* noop */ }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue(nameof(StatusCode), StatusCode);
        base.GetObjectData(info, context);
    }
}