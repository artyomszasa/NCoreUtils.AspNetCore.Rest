using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace NCoreUtils.AspNetCore.Rest.Serialization.Internal;

public class JsonContextBackedConverterFactory : JsonConverterFactory
{
    private JsonSerializerContext Context { get; }

    public JsonContextBackedConverterFactory(JsonSerializerContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public override bool CanConvert(Type typeToConvert)
        => Context.GetTypeInfo(typeToConvert) is not null;

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Compatibility only.")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(JsonContextBackedConverter<>))]
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        => Context.GetTypeInfo(typeToConvert) switch
        {
            null => throw new InvalidOperationException($"Configured context does not support serializing onstances of type {typeToConvert}."),
            var info => (JsonConverter?)Activator.CreateInstance(typeof(JsonContextBackedConverter<>).MakeGenericType(typeToConvert), info)
        };
}