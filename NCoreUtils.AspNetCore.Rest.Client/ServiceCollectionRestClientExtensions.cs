using Microsoft.Extensions.DependencyInjection;

namespace NCoreUtils.Rest
{
    public static class ServiceCollectionRestClientExtensions
    {
        public static IServiceCollection AddRestClientServices(this IServiceCollection services)
        {
            return services;
        }
    }
}