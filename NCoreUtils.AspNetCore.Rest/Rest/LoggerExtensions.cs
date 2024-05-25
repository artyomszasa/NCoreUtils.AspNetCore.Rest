using System;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.AspNetCore.Rest;

internal static partial class LoggingExtensions
{
    public const int RestEntityAlreadyExists = 11200;

    public const int RestEntityCreatedSuccessfully = 11201;

    public const int RestNoEntityFound = 11202;

    public const int RestEntityRemovedSuccessfully = 11203;

    public const int RestEntityUpdatedSuccessfully = 11204;

    public const int RestEntityAccessValidation = 11205;

    public const int RestQueryParsingDone = 11206;

    public const int RestUsingFallbackAsyncEnumerableSerializer = 11207;

    [LoggerMessage(
        EventId = RestEntityAlreadyExists,
        EventName = nameof(RestEntityAlreadyExists),
        Level = LogLevel.Debug,
        Message = "Entity of type {EntityType} with key = {Key} already exists (rest-create)."
    )]
    public static partial void LogRestEntityAlreadyExists(this ILogger logger, Type entityType, object? key);

    [LoggerMessage(
        EventId = RestEntityCreatedSuccessfully,
        EventName = nameof(RestEntityCreatedSuccessfully),
        Level = LogLevel.Information,
        Message = "Entity of type {EntityType} has been created with key = {Key} (rest-create).")]
    public static partial void LogRestEntityCreatedSuccessfully(this ILogger logger, Type entityType, object? key);

    [LoggerMessage(
        EventId = RestNoEntityFound,
        EventName = nameof(RestNoEntityFound),
        Level = LogLevel.Debug,
        Message = "No entity of type {EntityType} found for key = {Key} (rest-delete).")]
    public static partial void LogRestNoEntityFound(this ILogger logger, Type entityType, object? key);

    [LoggerMessage(
        EventId = RestEntityRemovedSuccessfully,
        EventName = nameof(RestEntityRemovedSuccessfully),
        Level = LogLevel.Debug,
        Message = "Successfully removed entity of type {EntityType} with key = {Key} (data-delete).")]
    public static partial void LogRestEntityRemovedSuccessfully(this ILogger logger, Type entityType, object? key);

    [LoggerMessage(
        EventId = RestEntityUpdatedSuccessfully,
        EventName = nameof(RestEntityUpdatedSuccessfully),
        Level = LogLevel.Information,
        Message = "Entity of type {EntityType} with key = {Key} has been updated (rest-update).")]
    public static partial void LogRestEntityUpdatedSuccessfully(this ILogger logger, Type entityType, object? key);

    [LoggerMessage(
        EventId = RestEntityAccessValidation,
        EventName = nameof(RestEntityAccessValidation),
        Level = LogLevel.Trace,
        Message = "[{Type}] Access validation ({AccessAllowed}).")]
    public static partial void LogRestEntityAccessValidation(this ILogger logger, string type, bool accessAllowed);

    [LoggerMessage(
        EventId = RestQueryParsingDone,
        EventName = nameof(RestQueryParsingDone),
        Level = LogLevel.Trace,
        Message = "[{Type}] REST query parsing done.")]
    public static partial void LogRestQueryParsingDone(this ILogger logger, string type);

    [LoggerMessage(
        EventId = RestUsingFallbackAsyncEnumerableSerializer,
        EventName = nameof(RestUsingFallbackAsyncEnumerableSerializer),
        Level = LogLevel.Warning,
        Message = "Using fallback async enumerable serialization, for .NET 7 or greater IAsyncEnumerable<...> types should be added to the context.")]
    public static partial void LogRestUsingFallbackAsyncEnumerableSerializer(this ILogger logger);
}