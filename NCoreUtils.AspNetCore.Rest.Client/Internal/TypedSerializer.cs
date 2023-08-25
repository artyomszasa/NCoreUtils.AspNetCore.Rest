using System;
using System.Text.Json.Serialization.Metadata;

namespace NCoreUtils.Rest.Internal;

[Obsolete("Use JsonTypeInfoSerializer instead.")]
public class TypedSerializer<T> : JsonTypeInfoSerializer<T>
{
    public TypedSerializer(string contentType, JsonTypeInfo<T> jsonTypeInfo)
        : base(contentType, jsonTypeInfo)
    { }
}