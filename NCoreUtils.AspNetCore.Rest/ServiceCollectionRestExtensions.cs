using System.Runtime.ExceptionServices;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.DependencyInjection;
using NCoreUtils.AspNetCore.Rest;
using NCoreUtils.AspNetCore.Rest.Internal;

namespace NCoreUtils.AspNetCore;

public static class ServiceCollectionRestExtensions
{
    internal sealed class RestErrorAccessor : IRestErrorAccessor
    {
        public ExceptionDispatchInfo? Error { get; set; }
    }

    public static IServiceCollection AddRestErrorAccessor(this IServiceCollection services)
        => services
            .AddScoped<IRestErrorAccessor, RestErrorAccessor>();

    public static IServiceCollection AddRestJsonTypeInfoResolver(this IServiceCollection services, IJsonTypeInfoResolver jsonTypeInfoResolver)
        => services
            .AddSingleton<IRestJsonTypeInfoResolver>(new RestJsonTypeInfoResolver(jsonTypeInfoResolver));
}