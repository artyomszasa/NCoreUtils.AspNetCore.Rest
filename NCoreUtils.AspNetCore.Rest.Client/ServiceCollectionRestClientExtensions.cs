using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NCoreUtils.Rest.Internal;

namespace NCoreUtils.Rest
{
    public static class ServiceCollectionRestClientExtensions
    {
        public static IServiceCollection AddRestClientServices(this IServiceCollection services)
        {
            services.TryAddSingleton<IRestTypeNameResolver, DefaultRestTypeNameResolver>();
            return services;
        }

        public static IServiceCollection AddDefaultRestClient(
            this IServiceCollection services,
            string endpoint,
            JsonSerializerContext jsonSerializerContext,
            string? httpClient = default)
        {
            var configuration = new RestClientConfiguration { Endpoint = endpoint };
            if (!string.IsNullOrEmpty(httpClient))
            {
                configuration.HttpClient = httpClient;
            }
            services
                .AddSingleton<IRestClientJsonSerializerContext>(new RestClientJsonSerializerContext(jsonSerializerContext))
                .AddSingleton<IRestClientConfiguration, RestClientConfiguration>()
                .AddSingleton<IRestClient, DefaultRestClient>()
                .AddSingleton<IHttpRestClient, HttpRestClient>();
            return services;
        }
    }
}