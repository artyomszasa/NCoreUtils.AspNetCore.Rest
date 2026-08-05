using Microsoft.Extensions.Logging;

namespace NCoreUtils.AspNetCore.Rest;

public partial class RestEndpointDataSource
{
    public const int ExceptionHasBeenHandledBy = 2000;

    public const int ExceptionHasBeenPassedBy = 2001;

    public const int ExceptionUnhandledBy = 2002;

    public const int ExceptionHandlerThrownException = 2003;

    public const int ExpectedErrorOccured = 2004;

    public const int ExpectedErrorOccuredWhenResponseHasBeenStarted = 2005;

    public const int ErrorOccured = 2006;

    public const int ErrorOccuredWhenResponseHasBeenStarted = 2007;

    [LoggerMessage(
        EventId = ExceptionHasBeenHandledBy,
        EventName = nameof(ExceptionHasBeenHandledBy),
        Level = LogLevel.Debug,
        Message = "Exception has been handled by {HandlerType}.")]
    public static partial void LogExceptionHasBeenHandledBy(ILogger logger, Type HandlerType);

    [LoggerMessage(
        EventId = ExceptionHasBeenPassedBy,
        EventName = nameof(ExceptionHasBeenPassedBy),
        Level = LogLevel.Debug,
        Message = "Exception has been passed by {HandlerType}.")]
    public static partial void LogExceptionHasBeenPassedBy(ILogger logger, Type HandlerType);

    [LoggerMessage(
        EventId = ExceptionUnhandledBy,
        EventName = nameof(ExceptionUnhandledBy),
        Level = LogLevel.Debug,
        Message = "{HandlerType} cannot handle the exception.")]
    public static partial void LogExceptionUnhandledBy(ILogger logger, Type HandlerType);

    [LoggerMessage(
        EventId = ExceptionHandlerThrownException,
        EventName = nameof(ExceptionHandlerThrownException),
        Level = LogLevel.Warning,
        Message = "Exception handler {HandlerType} thown an exception.")]
    public static partial void LogExceptionHandlerThrownException(ILogger logger, Exception exn, Type HandlerType);

    [LoggerMessage(
        EventId = ExpectedErrorOccured,
        EventName = nameof(ExpectedErrorOccured),
        Level = LogLevel.Debug,
        Message = "Expected error occured during endpoint execution (status code = {Code}).")]
    public static partial void LogExpectedErrorOccured(ILogger logger, Exception exn, int Code);

    [LoggerMessage(
        EventId = ExpectedErrorOccuredWhenResponseHasBeenStarted,
        EventName = nameof(ExpectedErrorOccuredWhenResponseHasBeenStarted),
        Level = LogLevel.Error,
        Message = "Expected error occured during endpoint execution (status code = {Code}) but response has been already started.")]
    public static partial void LogExpectedErrorOccuredWhenResponseHasBeenStarted(ILogger logger, Exception exn, int Code);

    [LoggerMessage(
        EventId = ErrorOccured,
        EventName = nameof(ErrorOccured),
        Level = LogLevel.Error,
        Message = "Error occured during endpoint execution.")]
    public static partial void LogErrorOccured(ILogger logger, Exception exn);

    [LoggerMessage(
        EventId = ErrorOccuredWhenResponseHasBeenStarted,
        EventName = nameof(ErrorOccuredWhenResponseHasBeenStarted),
        Level = LogLevel.Error,
        Message = "Error occured during endpoint execution and response has been already started.")]
    public static partial void LogErrorOccuredWhenResponseHasBeenStarted(ILogger logger, Exception exn);
}