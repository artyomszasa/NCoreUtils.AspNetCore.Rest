using System;
using System.Collections.Generic;
using NCoreUtils.Collections;

namespace NCoreUtils.AspNetCore.Rest.QueryParsers
{
    public static class RestQueryParserHelpers
    {
        public static void SplitCommaSeparatedStrings(ReadOnlySpan<char> input, ArrayPoolList<string> output)
        {
            var inString = false;
            var lastChar = '\0';
            var startIndex = 0;
            var i = 0;
            var l = input.Length;
            while (i < l)
            {
                var ch = input[i];
                switch (ch)
                {
                    case '"':
                        if (lastChar != '\\')
                        {
                            inString = !inString;
                        }
                        break;
                    case ',':
                        if (!inString)
                        {
                            output.Add(input.Slice(startIndex, i - startIndex).ToString());
                            startIndex = i + 1;
                        }
                        break;
                }
                lastChar = ch;
                ++i;
            }
            if (startIndex < i)
            {
                if (inString)
                {
                    throw new FormatException($"Invalid sort by string: \"{input.ToString()}\".");
                }
                output.Add(input.Slice(startIndex, i - startIndex).ToString());
            }
        }

        public static void ParseSortByDirections(ReadOnlySpan<char> input, ArrayPoolList<RestSortByDirection> output)
        {
            var startIndex = 0;
            var i = 0;
            var l = input.Length;
            while (i < l)
            {
                switch (input[i])
                {
                    case ',':
                        output.Add(Enum.Parse<RestSortByDirection>(input.Slice(startIndex, i - startIndex).ToString(), true));
                        startIndex = i + 1;
                        break;
                }
                ++i;
            }
            if (startIndex < i)
            {
                output.Add(Enum.Parse<RestSortByDirection>(input.Slice(startIndex, i - startIndex).ToString(), true));
            }
        }
    }
}