using System;

namespace NCoreUtils.Rest.Internal
{
    public static class RestClientConfiguationExtensions
    {
        public static string GetCollectionEndpoint<T>(
            this IRestClientConfiguration configuration,
            IRestTypeNameResolver nameResolver)
        {
            var name = nameResolver.ResolveTypeName(typeof(T));
            return configuration.Endpoint.EndsWith('/')
                ? configuration.Endpoint + name
                : configuration.Endpoint + '/' + name;
        }

        public static string GetItemOrReductionEndpoint<T>(
            this IRestClientConfiguration configuration,
            IRestTypeNameResolver nameResolver,
            string id)
        {
            var name = nameResolver.ResolveTypeName(typeof(T));
            return configuration.Endpoint.EndsWith('/')
                ? $"{configuration.Endpoint}{name}/{id}"
                : $"{configuration.Endpoint}/{name}/{id}";
        }
    }
}