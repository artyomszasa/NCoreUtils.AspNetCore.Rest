using System;
using NCoreUtils.AspNetCore.Rest.Internal;

namespace NCoreUtils.AspNetCore.Rest.Serialization;

public static class JsonTypeInfoDeserializerFactory
{
    public static IDeserializer<T> GetOrCreateDeserializer<T>(this IServiceProvider serviceProvider)
        => serviceProvider.GetOptionalService<IDeserializer<T>>() switch
        {
            null => serviceProvider.GetOptionalService<IRestJsonTypeInfoResolver>() switch
            {
                null => throw new InvalidOperationException($"No REST json serializer context has been registered and no explicit deserializer implementation has been provided."),
                var resolver => new JsonTypeInfoDeserializer<T>(resolver.GetJsonTypeInfoOrThrow<T>())
            },
            var serializer => serializer
        };
}