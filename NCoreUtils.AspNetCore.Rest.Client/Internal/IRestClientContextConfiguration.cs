using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using NCoreUtils.Data;

namespace NCoreUtils.Rest.Internal;

public interface IRestClientContextConfiguration
{
    string Endpoint { get; }

    string HttpClientConfigurationName { get; }

    TypedRestClient CreateClient(RestClientContextFactory factory);
}

internal interface IRestClientContextConfiguration<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TData, TId> : IRestClientContextConfiguration
    where TData : class, IHasId<TId>
    where TId : IEquatable<TId>
{
    IRestIdHandler<TId> IdHandler { get; }

    TypedRestClient IRestClientContextConfiguration.CreateClient(RestClientContextFactory factory)
        => new TypedRestClient<TData, TId>(
            factory.LoggerFactory.CreateLogger<TypedRestClient<TData, TId>>(),
            new RestClientContext<TData, TId>(
                factory.HttpClientFactory,
                factory.SerializerFactory.GetSerializer<TData>(),
                factory.QuerySerializer,
                IdHandler,
                Endpoint,
                HttpClientConfigurationName
            ),
            factory.ProtocolQueryProvider
        );
}
