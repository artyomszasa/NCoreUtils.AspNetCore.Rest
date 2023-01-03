using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NCoreUtils.AspNetCore.Rest.Internal;
using NCoreUtils.AspNetCore.Rest.Serialization.Internal;

namespace NCoreUtils.AspNetCore.Rest.Serialization;

public class JsonSerializerContextSerializerFactory : ISerializerFactory
{
    private abstract class Invoker
    {
        private static readonly ConcurrentDictionary<Type, Invoker> _cache = new ConcurrentDictionary<Type, Invoker>();

        private static readonly Func<Type, Invoker> _factory;

        [UnconditionalSuppressMessage("Trimming", "IL2111")]
        static Invoker()
        {
            _factory = DoCreate;
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Preserved by caller.")]
        [UnconditionalSuppressMessage("Trimming", "IL2111")]
        private static Invoker DoCreate([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type)
            => (Invoker)Activator.CreateInstance(typeof(Invoker<>).MakeGenericType(type), true)!;

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Invoker<>))]
        [UnconditionalSuppressMessage("Trimming", "IL2111")]
        public static ValueTask Serialize(JsonSerializerContextSerializerFactory self,
            IConfigurableOutput<Stream> configurableStream,
            object item,
            Type type,
            CancellationToken cancellationToken)
            => _cache.GetOrAdd(type, _factory)
                .DoSerialize(self, configurableStream, item, cancellationToken);

        protected abstract ValueTask DoSerialize(
            JsonSerializerContextSerializerFactory self,
            IConfigurableOutput<Stream> configurableStream,
            object item,
            CancellationToken cancellationToken);
    }

    private sealed class Invoker<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T> : Invoker
    {
        protected override ValueTask DoSerialize(
            JsonSerializerContextSerializerFactory self,
            IConfigurableOutput<Stream> configurableStream,
            object item,
            CancellationToken cancellationToken)
            => self.GetSerializer<T>().SerializeAsync(configurableStream, (T)item, cancellationToken);
    }

    private static bool IsAsyncEnumerable(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type,
        [MaybeNullWhen(false)] out Type elementType)
    {
        if (type.IsInterface && type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
        {
            elementType = type.GetGenericArguments()[0];
            return true;
        }
        elementType = default;
        return false;
    }

    internal static JsonSerializerContextSerializerFactory Create(IServiceProvider serviceProvider)
        => new JsonSerializerContextSerializerFactory(serviceProvider, serviceProvider.GetOptionalService<IRestJsonSerializerContext>());

    public IRestJsonSerializerContext? RestJsonSerializerContext { get; }

    public IServiceProvider ServiceProvider { get; }

    public JsonSerializerContextSerializerFactory(
        IServiceProvider serviceProvider,
        IRestJsonSerializerContext? restJsonSerializerContext = default)
    {
        RestJsonSerializerContext = restJsonSerializerContext;
        ServiceProvider = serviceProvider;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private T LogWarning<T>(T result)
    {
        ServiceProvider.GetRequiredService<ILogger<JsonSerializerContextSerializerFactory>>()
            .LogWarning("Using fallback async enumerable serialization, for .NET 7 or greater IAsyncEnumerable<...> types should be added to the context.");
        return result;
    }

    public ISerializer<T> GetSerializer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>()
        => ServiceProvider.GetOptionalService<ISerializer<T>>() switch
        {
            null => RestJsonSerializerContext switch
            {
                null => throw new InvalidOperationException("No REST json serializer context has been registered and no explicit serializer implementation has been provided."),
                // { JsonSerializerContext: var context } => IsAsyncEnumerable(typeof(T), out var elementType)
                //     ? new JsonContextBackedSerializer<T>(AsyncEnumerableSerializerOptions ??= new() { Converters = { new JsonContextBackedConverterFactory(context) } })
                //     : new JsonSerializerContextSerializer<T>(context)
                { JsonSerializerContext: var context } => context.GetTypeInfo(typeof(T)) switch
                {
                    null when IsAsyncEnumerable(typeof(T), out var elementType)
                        => LogWarning(new JsonContextBackedSerializer<T>(new() { Converters = { new JsonContextBackedConverterFactory(context) } })),
                    null => throw new InvalidOperationException($"Registered json serialier context return not json info for {typeof(T)}."),
                    JsonTypeInfo<T> jsonTypeInfo => new TypedJsonSerializer<T>(jsonTypeInfo),
                    _ => throw new ArgumentException($"Registered json serialier context returned invalid type info for {typeof(T)}.")
                }
            },
            var serializer => serializer
        };

    public ValueTask SerializeAsync(
        IConfigurableOutput<Stream> configurableStream,
        object item,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type,
        CancellationToken cancellationToken = default)
        => Invoker.Serialize(this, configurableStream, item, type, cancellationToken);
}