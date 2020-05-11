using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace NCoreUtils.AspNetCore.Rest
{
    public class DefaultSerializerFactory : ISerializerFactory
    {
        private abstract class Invoker
        {
            private static readonly ConcurrentDictionary<Type, Invoker> _cache = new ConcurrentDictionary<Type, Invoker>();

            private static readonly Func<Type, Invoker> _factory = type =>
                (Invoker)Activator.CreateInstance(typeof(Invoker<>).MakeGenericType(type), true)!;

            public static ValueTask Serialize(DefaultSerializerFactory self,
                IConfigurableOutput<Stream> configurableStream,
                object item,
                Type type,
                CancellationToken cancellationToken)
                => _cache.GetOrAdd(type, _factory)
                    .DoSerialize(self, configurableStream, item, cancellationToken);

            protected abstract ValueTask DoSerialize(
                DefaultSerializerFactory self,
                IConfigurableOutput<Stream> configurableStream,
                object item,
                CancellationToken cancellationToken);
        }

        private sealed class Invoker<T> : Invoker
        {
            protected override ValueTask DoSerialize(
                DefaultSerializerFactory self,
                IConfigurableOutput<Stream> configurableStream,
                object item,
                CancellationToken cancellationToken)
                => self.DoSerializeAsync(configurableStream, (T)item, cancellationToken);
        }

        protected IServiceProvider ServiceProvider { get; }

        public DefaultSerializerFactory(IServiceProvider serviceProvider)
            => ServiceProvider = serviceProvider;

        private ValueTask DoSerializeAsync<T>(
            IConfigurableOutput<Stream> configurableStream,
            T item,
            CancellationToken cancellationToken)
            => GetSerializer<T>().SerializeAsync(configurableStream, item, cancellationToken);

        public virtual ISerializer<T> GetSerializer<T>()
            => ServiceProvider.GetService<ISerializer<T>>() ?? ActivatorUtilities.CreateInstance<DefaultSerializer<T>>(ServiceProvider);

        public ValueTask SerializeAsync(IConfigurableOutput<Stream> configurableStream, object item, Type type, CancellationToken cancellationToken = default)
            => Invoker.Serialize(this, configurableStream, item, type, cancellationToken);
    }
}