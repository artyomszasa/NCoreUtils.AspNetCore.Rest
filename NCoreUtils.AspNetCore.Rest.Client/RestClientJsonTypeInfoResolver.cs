using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace NCoreUtils.Rest.Internal;

public class RestClientJsonTypeInfoResolver : IRestClientJsonTypeInfoResolver
{
    public IJsonTypeInfoResolver Resolver { get; }

    public JsonSerializerOptions DefaultOptions { get; }

    public RestClientJsonTypeInfoResolver(IJsonTypeInfoResolver resolver, JsonSerializerOptions? defaultOptions = default)
    {
        Resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        DefaultOptions = defaultOptions ?? new() { TypeInfoResolver = resolver, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    public JsonTypeInfo? GetTypeInfo(Type type)
        => GetTypeInfo(type, DefaultOptions);

    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
        => Resolver.GetTypeInfo(type, options);
}