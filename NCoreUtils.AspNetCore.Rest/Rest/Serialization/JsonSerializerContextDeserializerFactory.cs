using System;
using NCoreUtils.AspNetCore.Rest.Internal;

namespace NCoreUtils.AspNetCore.Rest.Serialization;

public static class JsonSerializerContextDeserializerFactory
{
    public static IDeserializer<T> GetOrCreateDeserializer<T>(this IServiceProvider serviceProvider)
        => serviceProvider.GetOptionalService<IDeserializer<T>>() switch
        {
            null => serviceProvider.GetOptionalService<IRestJsonSerializerContext>() switch
            {
                null => throw new InvalidOperationException($"No REST json serializer context has been registered and no explicit deserializer implementation has been provided."),
                { JsonSerializerContext: var context } => new JsonSerializerContextDeserializer<T>(context)
            },
            var serializer => serializer
        };
}