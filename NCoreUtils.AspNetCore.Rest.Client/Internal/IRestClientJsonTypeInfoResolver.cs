using System;
using System.Text.Json.Serialization.Metadata;

namespace NCoreUtils.Rest.Internal;

public interface IRestClientJsonTypeInfoResolver : IJsonTypeInfoResolver
{
    JsonTypeInfo? GetTypeInfo(Type type);
}