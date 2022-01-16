using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.AspNetCore.Rest.Internal;

namespace NCoreUtils.AspNetCore.Rest.Serialization;

public class JsonSerializerContextSerializerFactory : ISerializerFactory
{
    private abstract class Invoker
    {
        private static readonly ConcurrentDictionary<Type, Invoker> _cache = new ConcurrentDictionary<Type, Invoker>();

        private static readonly Func<Type, Invoker> _factory = DoCreate;

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Preserved by caller.")]
        private static Invoker DoCreate(Type type)
            => (Invoker)Activator.CreateInstance(typeof(Invoker<>).MakeGenericType(type), true)!;

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Invoker<>))]
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

    private sealed class Invoker<T> : Invoker
    {
        protected override ValueTask DoSerialize(
            JsonSerializerContextSerializerFactory self,
            IConfigurableOutput<Stream> configurableStream,
            object item,
            CancellationToken cancellationToken)
            => self.GetSerializer<T>().SerializeAsync(configurableStream, (T)item, cancellationToken);
    }

    internal static JsonSerializerContextSerializerFactory Create(IServiceProvider serviceProvider)
        => new JsonSerializerContextSerializerFactory(serviceProvider, serviceProvider.GetOptionalService<IRestJsonSerializerContext>());

    public IRestJsonSerializerContext? RestJsonSerializerContext { get; }

    public IServiceProvider ServiceProvider { get; }

    public JsonSerializerContextSerializerFactory(IServiceProvider serviceProvider, IRestJsonSerializerContext? restJsonSerializerContext = default)
    {
        RestJsonSerializerContext = restJsonSerializerContext;
        ServiceProvider = serviceProvider;
    }

    public ISerializer<T> GetSerializer<T>()
        => ServiceProvider.GetOptionalService<ISerializer<T>>() switch
        {
            null => RestJsonSerializerContext switch
            {
                null => throw new InvalidOperationException($"No REST json serializer context has been registered and no explicit serializer implementation has been provided."),
                { JsonSerializerContext: var context } => new JsonSerializerContextSerializer<T>(context)
            },
            var serializer => serializer
        };

    public ValueTask SerializeAsync(IConfigurableOutput<Stream> configurableStream, object item, Type type, CancellationToken cancellationToken = default)
        => Invoker.Serialize(this, configurableStream, item, type, cancellationToken);
}