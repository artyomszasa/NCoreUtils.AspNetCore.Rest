using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NCoreUtils.AspNetCore.Rest.Serialization.Internal;

public class JsonContextBackedConverterFactory(JsonSerializerContext context) : JsonConverterFactory
{
    private JsonSerializerContext Context { get; } = context ?? throw new ArgumentNullException(nameof(context));

    public override bool CanConvert(Type typeToConvert)
        => Context.GetTypeInfo(typeToConvert) is not null;

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Compatibility only.")]
    [UnconditionalSuppressMessage("Trimming", "IL3051", Justification = "Deprecated.")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(JsonContextBackedConverter<>))]
    [RequiresDynamicCode("Deprecated")]
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        => Context.GetTypeInfo(typeToConvert) switch
        {
            null => throw new InvalidOperationException($"Configured context does not support serializing instances of type {typeToConvert}."),
            var info => (JsonConverter?)Activator.CreateInstance(typeof(JsonContextBackedConverter<>).MakeGenericType(typeToConvert), info)
        };
}