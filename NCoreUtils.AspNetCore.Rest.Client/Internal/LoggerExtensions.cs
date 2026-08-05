using Microsoft.Extensions.Logging;

namespace NCoreUtils.Rest.Internal;

public static partial class LoggerExtensions
{
    private static class EventIds
    {
        public const int RestCollection = 9000;

        public const int RestCollectionUriResolved = 9001;

        public const int RestReductionUriResolved = 9002;
    }

    public static void LogRestCollection(
        this ILogger logger,
        string? target = default,
        string? filter = default,
        string? sortBy = default,
        string? sortByDirection = default,
        IReadOnlyList<string>? fields = default,
        IReadOnlyList<string>? includes = default,
        int offset = 0,
        int? limit = default)
    {
        Preconditions.ThrowIfNull(logger);
        logger.Log(
                LogLevel.Debug,
                new EventId(EventIds.RestCollection, nameof(EventIds.RestCollection)),
                new L.ListCollectionRequestData(target, filter, sortBy, sortByDirection, fields, includes, offset, limit),
                default,
                L.ListCollectionRequestData.LogFormatter
            );
    }

    [LoggerMessage(
        EventId = EventIds.RestCollectionUriResolved,
        EventName = nameof(EventIds.RestCollectionUriResolved),
        Level = LogLevel.Trace,
        Message = "Collection endpoint for {Type} has been resolved to {RequestUri}.")]
    public static partial void LogRestCollectionUriResolved(this ILogger logger, Type type, string requestUri);

    [LoggerMessage(
        EventId = EventIds.RestReductionUriResolved,
        EventName = nameof(EventIds.RestReductionUriResolved),
        Level = LogLevel.Trace,
        Message = "Reduction endpoint for {Type} has been resolved to {RequestUri}.")]
    public static partial void LogRestReductionUriResolved(this ILogger logger, Type type, string requestUri);
}