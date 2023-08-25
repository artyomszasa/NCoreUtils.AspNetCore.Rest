using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace NCoreUtils.AspNetCore.Rest.Internal;

public class RestJsonTypeInfoResolver : IRestJsonTypeInfoResolver
{
    private IJsonTypeInfoResolver Resolver { get; }

    public JsonSerializerOptions DefaultOptions { get; }

    public RestJsonTypeInfoResolver(IJsonTypeInfoResolver resolver, JsonSerializerOptions? options = default)
    {
        Resolver = resolver;
        DefaultOptions = options ?? new() { TypeInfoResolver = resolver, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
        => Resolver.GetTypeInfo(type, options);

    public JsonTypeInfo? GetTypeInfo(Type type)
        => GetTypeInfo(type, DefaultOptions);
}