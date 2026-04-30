using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NCoreUtils.AspNetCore.Rest.Unit.Data;
using NCoreUtils.Data;

namespace NCoreUtils.AspNetCore.Rest.Unit;

public class Startup
{
    private static readonly string[] stringsA = new string[] { "a" };

    private static readonly string[] stringsAB = new string[] { "a", "b" };

    private static readonly string[] stringsABC = new string[] { "a", "b", "c" };

    private static readonly string[] stringsABCD = new string[] { "a", "b", "c", "d" };

    public void ConfigureServices(IServiceCollection services)
    {
        services
            // HTTP CLIENT
            // .AddHttpClient()
            // HTTP CONTEXT
            .AddHttpContextAccessor()
            // JSON
            .AddRestJsonTypeInfoResolver(TestSerializerContext.Default)
            // DATA
            .AddDataQueryServices(TestQueryContext.Singleton)
            .AddInMemoryDataRepositoryContext()
            .AddInMemoryDataRepository<TestData, int>(new List<TestData>
            {
                new(1, "1", 1.0, stringsA),
                new(3, "3", 3.0, stringsABC),
                new(2, "2", 2.0, stringsAB),
                new(4, "1", 4.0, stringsABCD),
            })
            // ROUTING
            .AddRouting();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Only test")]
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app
            .UseRouting()
            .UseEndpoints(endpoints =>
            {
                endpoints.MapRestEndpoints(config =>
                {
                    config.AddEntity<TestData, int>();
                });
            });
    }

    // public IWebHostBuilder CreateWebHostBuilder(string[] args)
    // {
    //     return WebHost.CreateDefaultBuilder<Startup>(args);
    // }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Only test")]
    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(b => b.UseTestServer().UseStartup<Startup>());
    }
}