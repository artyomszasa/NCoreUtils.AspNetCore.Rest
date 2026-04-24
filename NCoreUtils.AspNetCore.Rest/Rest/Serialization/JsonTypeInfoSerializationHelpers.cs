using System;
using System.Text.Json.Serialization.Metadata;
using NCoreUtils.AspNetCore.Rest.Internal;

namespace NCoreUtils.AspNetCore.Rest.Serialization;

internal static class JsonTypeInfoSerializationHelpers
{
    public static JsonTypeInfo<T> GetJsonTypeInfoOrThrow<T>(this IRestJsonTypeInfoResolver resolver)
        => resolver.GetTypeInfo(typeof(T)) switch
        {
            null => throw new InvalidOperationException($"Registered json type info resolver return no json type info for {typeof(T)}."),
            JsonTypeInfo<T> jsonTypeInfo => jsonTypeInfo,
            _ => throw new ArgumentException($"Registered json type info resolver returned invalid type info for {typeof(T)}.")
        };
}