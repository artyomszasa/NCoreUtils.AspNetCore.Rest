using System;
using System.Collections.Generic;

namespace NCoreUtils.AspNetCore.Rest
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class PartialDataAttribute : Attribute
    {
        public Type SourceType { get; }

        public IReadOnlyList<string> FieldsSelector { get; }

        public PartialDataAttribute(Type sourceType, string[] fieldsSelector)
        {
            SourceType = sourceType;
            FieldsSelector = fieldsSelector;
        }
    }
}