using System.Collections.Generic;
using NCoreUtils.AspNetCore.Rest.QueryParsers;

namespace NCoreUtils.AspNetCore.Rest
{
    public class DefaultRestQueryParser : CompositeQueryParser
    {
        private static readonly IReadOnlyList<IRestQueryParser> _parsers = new IRestQueryParser[]
        {
            new HeaderRestQueryParser(),
            new QueryArgumentsRestQueryParser()
        };

        public DefaultRestQueryParser() : base(_parsers) { }
    }
}