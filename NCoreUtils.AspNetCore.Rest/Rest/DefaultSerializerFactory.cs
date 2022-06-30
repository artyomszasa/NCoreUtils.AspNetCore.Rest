using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace NCoreUtils.AspNetCore.Rest
{
    [Obsolete("JsonSerializerContext based seriialization is preferred.")]
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
    public class DefaultSerializerFactory : ISerializerFactory
    {
        private abstract class Invoker
        {
            private static readonly ConcurrentDictionary<Type, Invoker> _cache = new ConcurrentDictionary<Type, Invoker>();

            private static readonly Func<Type, Invoker> _factory = DoCreate;

            [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Obsolete member.")]
            private static Invoker DoCreate(Type type)
                => (Invoker)Activator.CreateInstance(typeof(Invoker<>).MakeGenericType(type), true)!;

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

        [UnconditionalSuppressMessage("Trimming", "IL2046")]
        public virtual ISerializer<T> GetSerializer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>()
            => ServiceProvider.GetService<ISerializer<T>>() ?? ActivatorUtilities.CreateInstance<DefaultSerializer<T>>(ServiceProvider);

        [UnconditionalSuppressMessage("Trimming", "IL2046")]
        public ValueTask SerializeAsync(
            IConfigurableOutput<Stream> configurableStream,
            object item,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type,
            CancellationToken cancellationToken = default)
            => Invoker.Serialize(this, configurableStream, item, type, cancellationToken);
    }
}