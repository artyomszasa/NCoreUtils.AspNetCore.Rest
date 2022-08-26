using System;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.AspNetCore.Rest;

public partial class RestEndpointDataSource
{
#if NET6_0_OR_GREATER
#pragma warning disable SYSLIB1006 // Multiple logging methods are using event id -1
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Exception has been handled by {HandlerType}.")]
    public static partial void LogExceptionHasBeenHandledBy(ILogger logger, Type HandlerType);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Exception has been passed by {HandlerType}.")]
    public static partial void LogExceptionHasBeenPassedBy(ILogger logger, Type HandlerType);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "{HandlerType} cannot handle the exception.")]
    public static partial void LogExceptionUnhandledBy(ILogger logger, Type HandlerType);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Exception handler {HandlerType} thown an exception.")]
    public static partial void LogExceptionHandlerThrownException(ILogger logger, Exception exn, Type HandlerType);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Expected error occured during endpoint execution (status code = {Code}).")]
    public static partial void LogExpectedErrorOccured(ILogger logger, Exception exn, int Code);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Expected error occured during endpoint execution (status code = {Code}) but response has been already started.")]
    public static partial void LogExpectedErrorOccuredWhenResponseHasBeenStarted(ILogger logger, Exception exn, int Code);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Error occured during endpoint execution.")]
    public static partial void LogErrorOccured(ILogger logger, Exception exn);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Error occured during endpoint execution and response has been already started.")]
    public static partial void LogErrorOccuredWhenResponseHasBeenStarted(ILogger logger, Exception exn);
#pragma warning restore SYSLIB1006
#else
    public static void LogExceptionHasBeenHandledBy(ILogger logger, Type HandlerType)
        => logger.LogDebug("Exception has been handled by {HandlerType}.", HandlerType);

    public static void LogExceptionHasBeenPassedBy(ILogger logger, Type HandlerType)
        => logger.LogDebug("Exception has been passed by {HandlerType}.", HandlerType);

    public static void LogExceptionUnhandledBy(ILogger logger, Type HandlerType)
        => logger.LogDebug("{HandlerType} cannot handle the exception.", HandlerType);

    public static void LogExceptionHandlerThrownException(ILogger logger, Exception exn, Type HandlerType)
        => logger.LogWarning(exn, "Exception handler {HandlerType} thown an exception.", HandlerType);

    public static void LogExpectedErrorOccured(ILogger logger, Exception exn, int Code)
        => logger.LogDebug(
            exn,
            "Expected error occured during endpoint execution (status code = {Code}).",
            Code
        );

    public static void LogExpectedErrorOccuredWhenResponseHasBeenStarted(ILogger logger, Exception exn, int Code)
        => logger.LogError(
            exn,
            "Expected error occured during endpoint execution (status code = {Code}) but response has been already stared.",
            Code
        );

    public static void LogErrorOccured(ILogger logger, Exception exn)
        => logger.LogError(exn, "Error occured during endpoint execution.");

    public static void LogErrorOccuredWhenResponseHasBeenStarted(ILogger logger, Exception exn)
        => logger.LogError(exn, "Error occured during endpoint execution and response has been already started.");
#endif
}