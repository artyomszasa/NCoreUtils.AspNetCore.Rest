using System;

namespace NCoreUtils.Rest.Internal
{
    public static class RestClientConfiguationExtensions
    {
        public static string GetCollectionEndpoint<T>(
            this IRestClientConfiguration configuration,
            IRestTypeNameResolver nameResolver)
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            if (nameResolver is null)
            {
                throw new ArgumentNullException(nameof(nameResolver));
            }
            var name = nameResolver.ResolveTypeName(typeof(T));
            if (configuration.Endpoint is null)
            {
                throw new InvalidOperationException($"configuration.Endpoint is null while resolving collection endpoint for {typeof(T)}.");
            }
            return configuration.Endpoint.EndsWith('/')
                ? configuration.Endpoint + name
                : configuration.Endpoint + '/' + name;
        }

        public static string GetItemOrReductionEndpoint<T>(
            this IRestClientConfiguration configuration,
            IRestTypeNameResolver nameResolver,
            string id)
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            if (nameResolver is null)
            {
                throw new ArgumentNullException(nameof(nameResolver));
            }
            var name = nameResolver.ResolveTypeName(typeof(T));
            return configuration.Endpoint.EndsWith('/')
                ? $"{configuration.Endpoint}{name}/{id}"
                : $"{configuration.Endpoint}/{name}/{id}";
        }
    }
}