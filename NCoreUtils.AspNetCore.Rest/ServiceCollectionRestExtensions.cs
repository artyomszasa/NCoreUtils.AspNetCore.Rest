using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.AspNetCore.Rest;

namespace NCoreUtils.AspNetCore
{
    public static class ServiceCollectionRestExtensions
    {
        internal sealed class RestErrorAccessor : IRestErrorAccessor
        {
            public ExceptionDispatchInfo? Error { get; set; }
        }

        public static IServiceCollection AddRestErrorAccessor(this IServiceCollection services)
            => services
                .AddScoped<IRestErrorAccessor, RestErrorAccessor>();
    }
}