using System.Net.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace NCoreUtils.AspNetCore.Rest.Unit;

public class TestHttpClientFactory : IHttpClientFactory
{
    private IHost TestHost { get; }

    public TestHttpClientFactory(IHost testHost)
    {
        TestHost = testHost;
    }

    public HttpClient CreateClient(string name)
        => TestHost.GetTestClient();
}