using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace NCoreUtils.AspNetCore.Rest.Internal;

public class RestJsonTypeInfoResolver(IJsonTypeInfoResolver resolver, JsonSerializerOptions? options = default)
    : IRestJsonTypeInfoResolver
{
    private IJsonTypeInfoResolver Resolver { get; } = resolver;

    public JsonSerializerOptions DefaultOptions { get; } = options ?? new() { TypeInfoResolver = resolver, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
        => Resolver.GetTypeInfo(type, options);

    public JsonTypeInfo? GetTypeInfo(Type type)
        => GetTypeInfo(type, DefaultOptions);
}