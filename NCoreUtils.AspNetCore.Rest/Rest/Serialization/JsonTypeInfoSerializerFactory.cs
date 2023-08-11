using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.AspNetCore.Rest.Internal;

namespace NCoreUtils.AspNetCore.Rest.Serialization;

public class JsonTypeInfoSerializerFactory : ISerializerFactory
{
    private abstract class Invoker
    {
        internal static ConcurrentDictionary<Type, Invoker> Cache { get; } = new();

        public static ValueTask Serialize(
            JsonTypeInfoSerializerFactory self,
            IConfigurableOutput<Stream> configurableStream,
            object item,
            Type type,
            CancellationToken cancellationToken)
            => Cache.TryGetValue(type, out var invoker)
                ? invoker.DoSerialize(self, configurableStream, item, cancellationToken)
                : throw new InvalidOperationException($"\"{type}\" has not been registered for dynamic serialization, use JsonTypeInfoSerializerFactory.RegisterSerializableType to register the type.");

        static Invoker()
        {
            Cache.TryAdd(typeof(bool), new Invoker<bool>());
            Cache.TryAdd(typeof(int), new Invoker<int>());
        }

        protected abstract ValueTask DoSerialize(
            JsonTypeInfoSerializerFactory self,
            IConfigurableOutput<Stream> configurableStream,
            object item,
            CancellationToken cancellationToken
        );
    }

    private sealed class Invoker<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T> : Invoker
    {
        protected override ValueTask DoSerialize(
            JsonTypeInfoSerializerFactory self,
            IConfigurableOutput<Stream> configurableStream,
            object item,
            CancellationToken cancellationToken)
            => self.GetSerializer<T>().SerializeAsync(configurableStream, (T)item, cancellationToken);
    }

    private static byte[] BinNull { get; } = "null"u8.ToArray();

    private static async ValueTask SerializeNullAsync(IConfigurableOutput<Stream> configurableStream, CancellationToken cancellationToken)
    {
        await using var stream = await configurableStream
            .InitializeAsync(new OutputInfo(default, "application/json; charset=utf-8"), cancellationToken)
            .ConfigureAwait(false);
        await stream.WriteAsync(BinNull, cancellationToken).ConfigureAwait(false);
    }

    internal static JsonTypeInfoSerializerFactory Create(IServiceProvider serviceProvider)
        => new(serviceProvider, serviceProvider.GetOptionalService<IRestJsonTypeInfoResolver>());

    public static void RegisterSerializableType<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>()
        => Invoker.Cache.TryAdd(typeof(T), new Invoker<T>());

    public virtual IRestJsonTypeInfoResolver? RestJsonTypeInfoResolver { get; }

    public IServiceProvider ServiceProvider { get; }

    public JsonTypeInfoSerializerFactory(IServiceProvider serviceProvider, IRestJsonTypeInfoResolver? restJsonTypeInfoResolver = default)
    {
        RestJsonTypeInfoResolver = restJsonTypeInfoResolver;
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public ISerializer<T> GetSerializer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>()
        => ServiceProvider.GetOptionalService<ISerializer<T>>() switch
        {
            null => RestJsonTypeInfoResolver switch
            {
                null => throw new InvalidOperationException("No REST json type info resolver has been registered and no explicit serializer implementation has been provided."),
                var resolver => new JsonTypeInfoSerializer<T>(resolver.GetJsonTypeInfoOrThrow<T>())
            },
            var serializer => serializer
        };

    public ValueTask SerializeAsync(
        IConfigurableOutput<Stream> configurableStream,
        object item,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type,
        CancellationToken cancellationToken = default)
        => item switch
        {
            null => SerializeNullAsync(configurableStream, cancellationToken),
            _ => Invoker.Serialize(this, configurableStream, item, type, cancellationToken)
        };
}