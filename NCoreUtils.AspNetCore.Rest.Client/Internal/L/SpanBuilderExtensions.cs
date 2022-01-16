using System.Collections.Generic;

namespace NCoreUtils.Rest.Internal.L
{
    internal static class SpanBuilderExtensions
    {
        public static bool TryAppendProperty(this ref SpanBuilder builder, ref bool first, string propertyName, int value)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                if (!builder.TryAppend(", ")) { return false; }
            }
            return builder.TryAppend(propertyName)
                && builder.TryAppend(" = ")
                && builder.TryAppend(value);
        }

        public static bool TryAppendProperty(this ref SpanBuilder builder, ref bool first, string propertyName, int? value)
        {
            if (!value.HasValue)
            {
                return true;
            }
            if (first)
            {
                first = false;
            }
            else
            {
                if (!builder.TryAppend(", ")) { return false; }
            }
            return builder.TryAppend(propertyName)
                && builder.TryAppend(" = ")
                && builder.TryAppend(value.Value);
        }

        public static bool TryAppendProperty(this ref SpanBuilder builder, ref bool first, string propertyName, string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return true;
            }
            if (first)
            {
                first = false;
            }
            else
            {
                if (!builder.TryAppend(", ")) { return false; }
            }
            return builder.TryAppend(propertyName)
                && builder.TryAppend(" = ")
                && builder.TryAppend(value!);
        }

        public static bool TryAppendProperty(this ref SpanBuilder builder, ref bool first, string propertyName, IReadOnlyList<string>? value)
        {
            if (value is null || value.Count == 0)
            {
                return true;
            }
            if (first)
            {
                first = false;
            }
            else
            {
                if (!builder.TryAppend(", ")) { return false; }
            }
            if (!(builder.TryAppend(propertyName) && builder.TryAppend(" = "))) { return false; }
            var firstItem = true;
            foreach (var item in value)
            {
                if (firstItem)
                {
                    firstItem = false;
                }
                else
                {
                    if (!builder.TryAppend(", ")) { return false; }
                }
                if (!builder.TryAppend(item)) { return false; }
            }
            return true;
        }
    }
}