using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace NCoreUtils.AspNetCore.Rest.Internal
{
    public class PartialClassInfo
    {
        public Type SourceType { get; }

        public IReadOnlyList<string> Fields { get; }

        public Type Type { get; }

        public LambdaExpression Selector { get; }

        public PartialClassInfo(Type sourceType, IReadOnlyList<string> fields, Type type, LambdaExpression selector)
        {
            SourceType = sourceType ?? throw new ArgumentNullException(nameof(sourceType));
            Fields = fields ?? throw new ArgumentNullException(nameof(fields));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Selector = selector ?? throw new ArgumentNullException(nameof(selector));
        }
    }
}