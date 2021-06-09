using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NCoreUtils.AspNetCore.Rest.QueryParsers
{
    public class CompositeQueryParser : IRestQueryParser
    {
        protected IReadOnlyList<IRestQueryParser> Parsers { get; }

        /// <summary>
        /// Initializes new instance of <see cref="CompositeQueryParser" /> from the specified parsers. Order of the
        /// parsers is significant as parsers are invoked sequentially and values retuned by the subsequent parser
        /// may override all the prevously returned values.
        /// </summary>
        /// <param name="parsers">List of parsers to use.</param>
        public CompositeQueryParser(IReadOnlyList<IRestQueryParser> parsers)
        {
            if (parsers is null)
            {
                throw new ArgumentNullException(nameof(parsers));
            }
            if (parsers.Count < 1)
            {
                throw new ArgumentException("At least on parser must be specified.", nameof(parsers));
            }
            Parsers = parsers;
        }

        public async ValueTask<RestQuery> ParseAsync(HttpRequest httpRequest, CancellationToken cancellationToken)
        {
            // NOTE: parsers.Count >= 1 (ctor invariant)
            var values = await Parsers[0].ParseAsync(httpRequest, cancellationToken);
            for (var i = 1; i < Parsers.Count; ++i)
            {
                using var nextValues = await Parsers[i].ParseAsync(httpRequest, cancellationToken);
                using var prevValues = values;
                values = prevValues.Override(nextValues);
            }
            return values;
        }
    }
}