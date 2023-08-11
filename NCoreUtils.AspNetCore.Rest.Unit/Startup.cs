using System.Collections.Generic;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NCoreUtils.AspNetCore.Rest.Internal;
using NCoreUtils.AspNetCore.Rest.Unit.Data;
using NCoreUtils.Data;

namespace NCoreUtils.AspNetCore.Rest.Unit;

public class Startup
{
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
            .AddInMemoryDataRepositoryContext()
            .AddInMemoryDataRepository<TestData, int>(new List<TestData>
            {
                new(1, "1", 1.0, new string[] { "a" }),
                new(2, "2", 2.0, new string[] { "a", "b" }),
                new(3, "3", 3.0, new string[] { "a", "b", "c" })
            })
            // ROUTING
            .AddRouting();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app
            .UseRouting()
            .UseEndpoints(endpoints =>
            {
                endpoints.MapRestEndpoints(config =>
                {
                    config.AddEntity<TestData>();
                });
            });
    }

    // public IWebHostBuilder CreateWebHostBuilder(string[] args)
    // {
    //     return WebHost.CreateDefaultBuilder<Startup>(args);
    // }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(b => b.UseTestServer().UseStartup<Startup>());
    }
}