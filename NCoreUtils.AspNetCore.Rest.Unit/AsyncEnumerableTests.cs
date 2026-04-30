using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NCoreUtils.AspNetCore.Rest.Unit.Data;
using NCoreUtils.Linq;
using NCoreUtils.Rest;
using Xunit;

namespace NCoreUtils.AspNetCore.Rest.Unit;

public sealed class AsyncEnumerableTests : IAsyncDisposable
{
    private IHost TestHost { get; }

    public AsyncEnumerableTests()
    {
        TestHost = Startup.CreateHostBuilder(Array.Empty<string>()).Build();
        TestHost.Start();
    }

    [Fact]
    public async Task Enumerate()
    {
        await using var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IHttpClientFactory>(new TestHttpClientFactory(TestHost))
            .AddCommonRestClientServices(TestSerializerContext.Default)
            .AddRemoteRestType<TestData, int>("/")
            // .AddDefaultRestClient("/", (IJsonTypeInfoResolver)TestSerializerContext.Default)
            .AddDataQueryServices(TestQueryContext.Singleton)
            .BuildServiceProvider(false);

        var restClient = serviceProvider.GetRequiredService<IRestClient<TestData, int>>();
        var items = await restClient.ListCollectionAsync().ToListAsync(default);
        Assert.NotNull(items);
        Assert.Equal(4, items.Count);

        items = await restClient.ListCollectionAsync(
            sortBy: "e => e.stringData",
            sortByDirection: "desc",
            thenBy: [new("e => e.id", "asc")]
        ).ToListAsync(default);
        Assert.NotNull(items);
        Assert.Equal(4, items.Count);
    }

    [Fact]
    public async Task Create()
    {
        await using var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IHttpClientFactory>(new TestHttpClientFactory(TestHost))
            .AddCommonRestClientServices(TestSerializerContext.Default)
            .AddRemoteRestType<TestData, int>("/")
            // .AddRestClientServices()
            // .AddDefaultRestClient("/", (IJsonTypeInfoResolver)TestSerializerContext.Default)
            .AddDataQueryServices(TestQueryContext.Singleton)
            .BuildServiceProvider(false);
        var restClient = serviceProvider.GetRequiredService<IRestClient<TestData, int>>();
        var x = await restClient.CreateAsync(new TestData(42, "xasd", 2.0, ["xxx"]));
    }

    public async ValueTask DisposeAsync()
    {
        await TestHost.StopAsync(default);
    }
}