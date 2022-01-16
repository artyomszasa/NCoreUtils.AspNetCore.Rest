using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Rest.Internal
{
    public static class LoggerExtensions
    {
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
            => logger.Log(
                LogLevel.Debug,
                default,
                new L.ListCollectionRequestData(target, filter, sortBy, sortByDirection, fields, includes, offset, limit),
                default,
                L.ListCollectionRequestData.LogFormatter
            );

    }
}