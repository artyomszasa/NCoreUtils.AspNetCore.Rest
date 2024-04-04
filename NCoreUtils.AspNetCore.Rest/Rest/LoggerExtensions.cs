using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.AspNetCore.Rest;

internal static partial class LoggingExtensions
{
    public const int RestEntityAlreadyExists = 11200;

    public const int RestEntityCreatedSuccessfully = 11201;

    public const int RestNoEntityFound = 11202;

    public const int RestEntityRemovedSuccessfully = 11203;

    public const int RestEntityUpdatedSuccessfully = 11204;

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
            Message = "Entity of type {EntityType} has been created with key = {Key} (rest-create)."
        )]
    public static partial void LogRestEntityCreatedSuccessfully(this ILogger logger, Type entityType, object? key);

    [LoggerMessage(
            EventId = RestNoEntityFound,
            EventName = nameof(RestNoEntityFound),
            Level = LogLevel.Debug,
            Message = "No entity of type {EntityType} found for key = {Key} (rest-delete)."
        )]
    public static partial void LogRestNoEntityFound(this ILogger logger, Type entityType, object? key);

    [LoggerMessage(
            EventId = RestEntityRemovedSuccessfully,
            EventName = nameof(RestEntityRemovedSuccessfully),
            Level = LogLevel.Information,
            Message = "Entity of type {EntityType} has been created with key = {Key} (rest-create)."
        )]
    public static partial void LogRestEntityRemovedSuccessfully(this ILogger logger, Type entityType, object? key);

    [LoggerMessage(
            EventId = RestEntityUpdatedSuccessfully,
            EventName = nameof(RestEntityUpdatedSuccessfully),
            Level = LogLevel.Information,
            Message = "Entity of type {EntityType} with key = {Key} has been updated (rest-update)."
        )]
    public static partial void LogRestEntityUpdatedSuccessfully(this ILogger logger, Type entityType, object? key);

}