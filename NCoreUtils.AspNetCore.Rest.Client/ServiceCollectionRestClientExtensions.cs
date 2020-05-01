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
    }
}