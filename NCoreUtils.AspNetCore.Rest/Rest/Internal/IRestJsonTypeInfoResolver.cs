using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace NCoreUtils.AspNetCore.Rest.Internal;

public interface IRestJsonTypeInfoResolver : IJsonTypeInfoResolver
{
    JsonSerializerOptions DefaultOptions { get; }

    JsonTypeInfo? GetTypeInfo(Type type);
}