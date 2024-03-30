using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NCoreUtils.Data;
using NCoreUtils.Rest.Internal;

#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#else
using System.Collections.Immutable;
#endif

namespace NCoreUtils.Rest;

public static partial class ServiceCollectionRestClientExtensions
{
    private sealed record DynamicSerializerFactory(IServiceProvider ServiceProvider, ISerializerFactory DefaultFactory)
        : ISerializerFactory
    {
        public string? ContentType => DefaultFactory.ContentType;

        public ISerializer<T> GetSerializer<T>()
            => ServiceProvider.GetService<ISerializer<T>>() switch
            {
                null => DefaultFactory.GetSerializer<T>(),
                var serializer => serializer
            };
    }


    private static RestClientContextFactory CreateRestClientContextFactory(IServiceProvider serviceProvider)
    {
        var configurations = new Dictionary<Type, IRestClientContextConfiguration>();
        foreach (var registration in serviceProvider.GetServices<RemoteRestTypeRegistration>())
        {
            configurations.Add(registration.DataType, registration.CreateConfiguration());
        }
        return new RestClientContextFactory(
#if NET8_0_OR_GREATER
            configurations: configurations.ToFrozenDictionary(),
#else
            configurations: configurations.ToImmutableDictionary(),
#endif
            loggerFactory: serviceProvider.GetRequiredService<ILoggerFactory>(),
            httpClientFactory: serviceProvider.GetRequiredService<IHttpClientFactory>(),
            querySerializer: serviceProvider.GetService<IRestQuerySerializer>() ?? new RestQueryAsQueryParameterSerializer(),
            serializerFactory: new DynamicSerializerFactory(
                serviceProvider,
                serviceProvider.GetService<ISerializerFactory>()
                    ?? new DefaultJsonSerializerFactory(
                        logger: serviceProvider.GetRequiredService<ILogger<DefaultJsonSerializerFactory>>(),
                        resolver: serviceProvider.GetRequiredService<IRestClientJsonTypeInfoResolver>()
                    )
            )
        );
    }

    public static IServiceCollection AddRemoteRestType<TData, TId>(
        this IServiceCollection services,
        string endpoint,
        string? httpClientConfigurationName = default,
        IRestIdHandler<TId>? idHandler = default)
        where TData : class, IHasId<TId>
        where TId : IEquatable<TId>
    {
        services.AddSingleton<RemoteRestTypeRegistration>(serviceProvider => new RemoteRestTypeRegistration<TData, TId>(
            endpoint,
            httpClientConfigurationName ?? "NCoreUtilsRestClient",
            idHandler ?? RestIdHandler.For<TId>(),
            serviceProvider.GetService<IRestTypeNameResolver>()
        ));
        services.AddScoped<IRestClient<TData, TId>>(serviceProvider =>
        {
            var factory = serviceProvider.GetRequiredService<RestClientContextFactory>();
            return (TypedRestClient<TData, TId>)factory.CreateClient(typeof(TData));
        });
        return services;
    }

    public static IServiceCollection AddCommonRestClientServices(this IServiceCollection services, IRestClientJsonTypeInfoResolver? jsonTypeInfoResolver = default)
    {
        if (jsonTypeInfoResolver is not null)
        {
            services.TryAddSingleton(jsonTypeInfoResolver);
        }
        services.TryAddSingleton(CreateRestClientContextFactory);
        services.TryAddSingleton<IRestDataQueryExecutor, TypedRestQueryExecutor>();
        return services;
    }

    public static IServiceCollection AddCommonRestClientServices(this IServiceCollection services, IJsonTypeInfoResolver jsonTypeInfoResolver)
        => services.AddCommonRestClientServices(new RestClientJsonTypeInfoResolver(jsonTypeInfoResolver));
}