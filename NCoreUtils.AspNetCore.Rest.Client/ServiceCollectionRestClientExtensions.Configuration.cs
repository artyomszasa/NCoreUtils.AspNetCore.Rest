using System;
using NCoreUtils.Data;
using NCoreUtils.Rest.Internal;

namespace NCoreUtils.Rest;

public static partial class ServiceCollectionRestClientExtensions
{
    private abstract class RemoteRestTypeRegistration
    {
        public abstract Type DataType { get; }

        public abstract IRestClientContextConfiguration CreateConfiguration();
    }

    private abstract class RemoteRestTypeRegistration<TData>(
        string endpoint,
        string httpClientConfigurationName,
        IRestTypeNameResolver? nameResolver = default)
        : RemoteRestTypeRegistration
    {
        public IRestTypeNameResolver NameResolver { get; } = nameResolver ?? DefaultRestTypeNameResolver.Singleton;

        public string Endpoint { get; } = endpoint;

        public string HttpClientConfigurationName { get; } = httpClientConfigurationName;

        public sealed override Type DataType => typeof(TData);

        protected string GetTypeName() => NameResolver.ResolveTypeName(typeof(TData));

        protected string GetTypeEndpoint() => $"{Endpoint.TrimEnd('/')}/{GetTypeName()}";
    }

    private sealed class RemoteRestTypeRegistration<TData, TId>(
        string endpoint,
        string httpClientConfigurationName,
        IRestIdHandler<TId> idHandler,
        IRestTypeNameResolver? nameResolver = default)
        : RemoteRestTypeRegistration<TData>(endpoint, httpClientConfigurationName, nameResolver)
        where TData : class, IHasId<TId>
        where TId : IEquatable<TId>
    {
        public IRestIdHandler<TId> IdHandler { get; } = idHandler ?? throw new ArgumentNullException(nameof(idHandler));

        public override IRestClientContextConfiguration CreateConfiguration()
            => new RestClientContextConfiguration<TData, TId>(GetTypeEndpoint(), HttpClientConfigurationName, IdHandler);
    }

    private sealed record RestClientContextConfiguration<TData, TId>(string Endpoint, string HttpClientConfigurationName, IRestIdHandler<TId> IdHandler)
        : IRestClientContextConfiguration<TData, TId>
        where TData : class, IHasId<TId>
        where TId : IEquatable<TId>;
}