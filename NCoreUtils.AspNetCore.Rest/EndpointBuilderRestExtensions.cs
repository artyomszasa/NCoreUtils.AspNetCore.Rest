using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using NCoreUtils.AspNetCore.Rest;

namespace NCoreUtils.AspNetCore
{
    public static class EndpointBuilderRestExtensions
    {
        public static IEndpointConventionBuilder MapRest(
            this IEndpointRouteBuilder builder,
            RestConfiguration configuration)
        {
            var dataSource = new RestEndpointDataSource(configuration);
            builder.DataSources.Add(dataSource);
            return dataSource;
        }

        public static IEndpointConventionBuilder MapRest(
            this IEndpointRouteBuilder builder,
            string prefix,
            Action<RestConfigurationBuilder> configure)
        {
            var configurationBuilder = new RestConfigurationBuilder();
            if (!string.IsNullOrEmpty(prefix))
            {
                configurationBuilder.WithPrefix(prefix);
            }
            configure?.Invoke(configurationBuilder);
            return builder.MapRest(configurationBuilder.Build());
        }

        public static IEndpointConventionBuilder MapRest(
            this IEndpointRouteBuilder builder,
            Action<RestConfigurationBuilder> configure)
            => builder.MapRest(string.Empty, configure);
    }
}