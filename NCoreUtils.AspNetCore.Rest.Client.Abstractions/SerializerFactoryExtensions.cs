using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Rest
{
    public static class SerializerFactoryExtensions
    {
        private abstract class Invoker
        {
            public abstract ValueTask<object> DeserializeAsync(
                ISerializerFactory factory,
                Stream stream,
                CancellationToken cancellationToken = default);

            public abstract ValueTask SerializeAsync(
                ISerializerFactory factory,
                Stream stream,
                object value,
                CancellationToken cancellationToken = default);
        }

        private sealed class Invoker<T> : Invoker
        {
            public override async ValueTask<object> DeserializeAsync(
                ISerializerFactory factory,
                Stream stream,
                CancellationToken cancellationToken = default)
                => (await factory.GetSerializer<T>().DeserializeAsync(stream, cancellationToken))!;

            public override ValueTask SerializeAsync(
                ISerializerFactory factory,
                Stream stream,
                object value,
                CancellationToken cancellationToken = default)
                => factory.GetSerializer<T>().SerializeAsync(stream, (T)value, cancellationToken);
        }

        private static ConcurrentDictionary<Type, Invoker> _cache = new ConcurrentDictionary<Type, Invoker>();

        private static Func<Type, Invoker> _factory = type =>
            (Invoker)Activator.CreateInstance(typeof(Invoker<>).MakeGenericType(type), true)!;

        public static ValueTask<object> DeserializeAsync(
            this ISerializerFactory factory,
            Stream stream,
            Type returnType,
            CancellationToken cancellationToken = default)
            => _cache.GetOrAdd(returnType, _factory)
                .DeserializeAsync(factory, stream, cancellationToken);

        public static ValueTask SetializeAsync(
            this ISerializerFactory factory,
            Stream stream,
            object value,
            Type inputType,
            CancellationToken cancellationToken = default)
            => _cache.GetOrAdd(inputType, _factory)
                .SerializeAsync(factory, stream, value, cancellationToken);

        public static ValueTask<T> DeserializeAsync<T>(
            this ISerializerFactory factory,
            Stream stream,
            CancellationToken cancellationToken = default)
            => factory.GetSerializer<T>().DeserializeAsync(stream, cancellationToken);

        public static ValueTask SerializeAsync<T>(
            this ISerializerFactory factory,
            Stream stream,
            T value,
            CancellationToken cancellationToken = default)
            => factory.GetSerializer<T>().SerializeAsync(stream, value, cancellationToken);
    }
}