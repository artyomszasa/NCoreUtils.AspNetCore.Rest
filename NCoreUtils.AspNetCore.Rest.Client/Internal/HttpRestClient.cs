using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NCoreUtils.Data;

namespace NCoreUtils.Rest.Internal
{
    public class HttpRestClient : IHttpRestClient
    {
        private static readonly JsonSerializerOptions _defaultJsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        protected IRestClientConfiguration Configuration { get; }

        protected IRestTypeNameResolver NameResolver;

        protected ISerializerFactory SerializerFactory;

        protected ILogger Logger;

        private readonly IHttpClientFactory? _httpClientFactory;

        public HttpRestClient(
            IServiceProvider serviceProvider,
            IRestClientConfiguration configuration,
            IRestTypeNameResolver nameResolver,
            ILogger<HttpRestClient> logger,
            ISerializerFactory? serializerFactory = default,
            IHttpClientFactory? httpClientFactory = default)
        {
            if (serviceProvider is null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            NameResolver = nameResolver ?? throw new ArgumentNullException(nameof(nameResolver));
            SerializerFactory = serializerFactory ?? ActivatorUtilities.CreateInstance<DefaultSerializerFactory>(serviceProvider);
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClientFactory = httpClientFactory;
        }

        protected virtual void HandleErrors(HttpResponseMessage response, string requestUri)
        {
            if (response is null)
            {
                throw new ArgumentNullException(nameof(response));
            }
            // check X-Message header
            if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.InternalServerError)
            {
                if (!(response.Headers is null) && response.Headers.TryGetValues("X-Message", out var values))
                {
                    var message = string.Join(" ", values.Select(Uri.UnescapeDataString));
                    throw new RestException(requestUri ?? string.Empty, message);
                }
            }
            // fallback to non-informational exception if failed...
            response.EnsureSuccessStatusCode();
        }

        protected TId ParseLocation<TData, TId>(string location, string requestUri)
        {
            if (location.StartsWith(Configuration.Endpoint))
            {
                var index = Configuration.Endpoint.Length;
                while (index < location.Length && location[index] == '/')
                {
                    ++index;
                }
                var typename = NameResolver.ResolveTypeName(typeof(TData));
                if (location.AsSpan().Slice(index).StartsWith(typename.AsSpan()))
                {
                    index += typename.Length;
                    while (index < location.Length && location[index] == '/')
                    {
                        ++index;
                    }
                    return (TId)Convert.ChangeType(location.Substring(index), typeof(TId));
                }
            }
            throw new RestException(requestUri, $"REST CREATE returned invalid location: {location}.");
        }

        protected virtual async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using var client = CreateClient();
            return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }

        protected virtual HttpClient CreateClient()
            => _httpClientFactory?.CreateClient(Configuration.HttpClient) ?? new HttpClient();

        public virtual async Task<IReadOnlyList<T>> ListCollectionAsync<T>(
            string? target = default,
            string? filter = default,
            string? sortBy = default,
            string? sortByDirection = default,
            IReadOnlyList<string>? fields = default,
            IReadOnlyList<string>? includes = default,
            int offset = 0,
            int? limit = default,
            CancellationToken cancellationToken = default)
        {
            var requestUri = Configuration.GetCollectionEndpoint<T>(NameResolver);
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            if (!string.IsNullOrEmpty(filter))
            {
                request.Headers.Add("X-Filter", Uri.EscapeDataString(filter));
            }
            if (!string.IsNullOrEmpty(sortBy))
            {
                request.Headers.Add("X-Sort-By", Uri.EscapeDataString(sortBy));
                request.Headers.Add("X-Sort-By-Direction", sortByDirection);
            }
            request.Headers.Add("X-Offset", offset.ToString(CultureInfo.InvariantCulture));
            if (limit.HasValue)
            {
                request.Headers.Add("X-Count", limit.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (!string.IsNullOrEmpty(target))
            {
                request.Headers.Add("X-Type", target);
            }
            using var response = await SendAsync(request, cancellationToken);
            HandleErrors(response, requestUri);
            #if NETSTANDARD2_1
            await using var stream = await response.Content.ReadAsStreamAsync();
            #else
            using var stream = await response.Content.ReadAsStreamAsync();
            #endif
            return await SerializerFactory.DeserializeAsync<List<T>>(stream, cancellationToken);
        }

        public virtual async Task<TData> ItemAsync<TData, TId>(
            TId id,
            CancellationToken cancellationToken = default)
            where TData : IHasId<TId>
        {
            var requestUri = Configuration.GetItemOrReductionEndpoint<TData>(NameResolver, Convert.ToString(id, CultureInfo.InvariantCulture));
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            using var response = await SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return default!;
            }
            HandleErrors(response, requestUri);
            #if NETSTANDARD2_1
            await using var stream = await response.Content.ReadAsStreamAsync();
            #else
            using var stream = await response.Content.ReadAsStreamAsync();
            #endif
            return await SerializerFactory.DeserializeAsync<TData>(stream, cancellationToken);
        }

        public virtual async Task<TId> CreateAsync<TData, TId>(
            TData data,
            CancellationToken cancellationToken = default)
            where TData : IHasId<TId>
        {
            var requestUri = Configuration.GetCollectionEndpoint<TData>(NameResolver);
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new SerializedContent<TData>(data, SerializerFactory.GetSerializer<TData>())
            };
            using var response = await SendAsync(request, cancellationToken);
            HandleErrors(response, requestUri);
            if (!response.Headers.TryGetValues("location", out var locationValues))
            {
                throw new RestException(requestUri, "REST CREATE returned no location.");
            }
            var id = ParseLocation<TData, TId>(locationValues.First(), requestUri);
            return id;
        }

        public virtual async Task UpdateAsync<TData, TId>(
            TId id,
            TData data,
            CancellationToken cancellationToken = default)
            where TData : IHasId<TId>
        {
            if (!IdUtils.IsValidId(id))
            {
                throw new InvalidOperationException($"Invalid id.");
            }
            if (!id!.Equals(data.Id))
            {
                throw new InvalidOperationException($"Invalid id.");
            }
            var requestUri = Configuration.GetItemOrReductionEndpoint<TData>(NameResolver, Convert.ToString(data.Id, CultureInfo.InvariantCulture));
            using var request = new HttpRequestMessage(HttpMethod.Put, requestUri)
            {
                Content = new SerializedContent<TData>(data, SerializerFactory.GetSerializer<TData>())
            };
            using var response = await SendAsync(request, cancellationToken);
            HandleErrors(response, requestUri);
        }

        public virtual async Task DeleteAsync<TData, TId>(TId id, CancellationToken cancellationToken = default)
            where TData : IHasId<TId>
        {
            var requestUri = Configuration.GetItemOrReductionEndpoint<TData>(NameResolver, Convert.ToString(id, CultureInfo.InvariantCulture));
            using var request = new HttpRequestMessage(HttpMethod.Delete, requestUri);
            using var response = await SendAsync(request, cancellationToken);
            HandleErrors(response, requestUri);
        }

        public virtual async Task<object> ReductionAsync<T>(
            string reduction,
            string? target = null,
            string? filter = null,
            string? sortBy = null,
            string? sortByDirection = null,
            int offset = 0,
            int? limit = null,
            CancellationToken cancellationToken = default)
        {
            var resultType = reduction switch
            {
                null => throw new ArgumentNullException(nameof(reduction)),
                "first" => typeof(T),
                "single" => typeof(T),
                "count" => typeof(int),
                _ => throw new NotSupportedException($"Not dupported reduction: {reduction}.")
            };
            var requestUri = Configuration.GetItemOrReductionEndpoint<T>(NameResolver, reduction);
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            if (!string.IsNullOrEmpty(filter))
            {
                request.Headers.Add("X-Filter", Uri.EscapeDataString(filter));
            }
            if (!string.IsNullOrEmpty(sortBy))
            {
                request.Headers.Add("X-Sort-By", Uri.EscapeDataString(sortBy));
                request.Headers.Add("X-Sort-By-Direction", sortByDirection);
            }
            request.Headers.Add("X-Offset", offset.ToString(CultureInfo.InvariantCulture));
            if (limit.HasValue)
            {
                request.Headers.Add("X-Count", limit.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (!string.IsNullOrEmpty(target))
            {
                request.Headers.Add("X-Type", target);
            }
            using var response = await SendAsync(request, cancellationToken);
            HandleErrors(response, requestUri);
            if (HttpStatusCode.NoContent == response.StatusCode)
            {
                return resultType.IsValueType ? Activator.CreateInstance(resultType) : null!;
            }
            #if NETSTANDARD2_1
            await using var stream = await response.Content.ReadAsStreamAsync();
            #else
            using var stream = await response.Content.ReadAsStreamAsync();
            #endif
            return await SerializerFactory.DeserializeAsync(stream, resultType, cancellationToken);
        }
    }
}