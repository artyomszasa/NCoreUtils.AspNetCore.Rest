using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NCoreUtils.Data;
using NCoreUtils.Rest.Internal;

namespace NCoreUtils.Rest;

public static partial class ServiceCollectionRestClientExtensions
{
    private abstract class RemoteRestTypeRegistration(IReadOnlyList<IRestClientErrorHandler> errorHandlers)
    {
        [SuppressMessage("Style", "IDE0301:Simplify collection initialization", Justification = "Potential array optimizations.")]
        public IReadOnlyList<IRestClientErrorHandler> ErrorHandlers { get; }
            = errorHandlers ?? Array.Empty<IRestClientErrorHandler>();

        public abstract Type DataType { get; }

        public abstract IRestClientContextConfiguration CreateConfiguration();
    }

    private abstract class RemoteRestTypeRegistration<TData>(
        IReadOnlyList<IRestClientErrorHandler> errorHandlers,
        string endpoint,
        string httpClientConfigurationName,
        IRestTypeNameResolver? nameResolver = default)
        : RemoteRestTypeRegistration(errorHandlers)
    {
        public IRestTypeNameResolver NameResolver { get; } = nameResolver ?? DefaultRestTypeNameResolver.Singleton;

        public string Endpoint { get; } = endpoint;

        public string HttpClientConfigurationName { get; } = httpClientConfigurationName;

        public sealed override Type DataType => typeof(TData);

        protected string GetTypeName() => NameResolver.ResolveTypeName(typeof(TData));

        protected string GetTypeEndpoint() => $"{Endpoint.TrimEnd('/')}/{GetTypeName()}";
    }

    private sealed class RemoteRestTypeRegistration<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TData, TId>(
        IReadOnlyList<IRestClientErrorHandler> errorHandlers,
        string endpoint,
        string httpClientConfigurationName,
        IRestIdHandler<TId> idHandler,
        IRestTypeNameResolver? nameResolver = default)
        : RemoteRestTypeRegistration<TData>(errorHandlers, endpoint, httpClientConfigurationName, nameResolver)
        where TData : class, IHasId<TId>
        where TId : IEquatable<TId>
    {
        public IRestIdHandler<TId> IdHandler { get; } = idHandler ?? throw new ArgumentNullException(nameof(idHandler));

        public override IRestClientContextConfiguration CreateConfiguration()
            => new RestClientContextConfiguration<TData, TId>(ErrorHandlers, GetTypeEndpoint(), HttpClientConfigurationName, IdHandler);
    }

    private sealed record RestClientContextConfiguration<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TData, TId>(
        IReadOnlyList<IRestClientErrorHandler> ErrorHandlers,
        string Endpoint,
        string HttpClientConfigurationName,
        IRestIdHandler<TId> IdHandler)
        : IRestClientContextConfiguration<TData, TId>
        where TData : class, IHasId<TId>
        where TId : IEquatable<TId>;
}