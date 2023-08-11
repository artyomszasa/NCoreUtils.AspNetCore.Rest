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

        [Obsolete("Use MapRestEndpoints instead.")]
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

        [Obsolete("Use MapRestEndpoints instead.")]
        public static IEndpointConventionBuilder MapRest(
            this IEndpointRouteBuilder builder,
            Action<RestConfigurationBuilder> configure)
            => builder.MapRest(string.Empty, configure);

        public static IEndpointConventionBuilder MapRestEndpoints(
            this IEndpointRouteBuilder builder,
            string prefix,
            Action<RestEndpointsConfigurationBuilder> configure)
        {
            var configurationBuilder = new RestEndpointsConfigurationBuilder();
            if (!string.IsNullOrEmpty(prefix))
            {
                configurationBuilder.WithPrefix(prefix);
            }
            configure?.Invoke(configurationBuilder);
            return builder.MapRest(configurationBuilder.Build());
        }

        public static IEndpointConventionBuilder MapRestEndpoints(
            this IEndpointRouteBuilder builder,
            Action<RestEndpointsConfigurationBuilder> configure)
            => builder.MapRestEndpoints(string.Empty, configure);
    }
}