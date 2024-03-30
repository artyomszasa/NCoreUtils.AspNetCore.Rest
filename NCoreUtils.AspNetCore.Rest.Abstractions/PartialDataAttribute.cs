using System;
using System.Collections.Generic;

namespace NCoreUtils.AspNetCore.Rest;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
[Obsolete("Customize JsonTypeInfoResolver instead.")]
public class PartialDataAttribute(Type sourceType, string[] fieldsSelector) : Attribute
{
    public Type SourceType { get; } = sourceType;

    public IReadOnlyList<string> FieldsSelector { get; } = fieldsSelector;
}