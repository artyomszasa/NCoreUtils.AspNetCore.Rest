using System;
using System.Net.Http;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NCoreUtils.AspNetCore.Rest.Unit.Data;
using NCoreUtils.Linq;
using NCoreUtils.Rest;
using Xunit;

namespace NCoreUtils.AspNetCore.Rest.Unit;

public class AsyncEnumerableTests : IAsyncDisposable
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
            .AddRestClientServices()
            .AddDefaultRestClient("/", (IJsonTypeInfoResolver)TestSerializerContext.Default)
            .AddDataQueryServices(TestQueryContext.Singleton)
            .BuildServiceProvider(false);

        var restClient = serviceProvider.GetRequiredService<IRestClient>();
        var items = await restClient.Collection<TestData>().ToListAsync(default);
        Assert.NotNull(items);
        Assert.Equal(3, items.Count);
    }

    public async ValueTask DisposeAsync()
    {
        await TestHost.StopAsync(default);
    }
}