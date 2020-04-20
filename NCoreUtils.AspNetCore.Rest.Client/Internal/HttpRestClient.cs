using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

        private readonly IRestClientConfiguration _configuration;

        private readonly IRestTypeNameResolver _nameResolver;

        private readonly ISerializerFactory _serializerFactory;

        private readonly ILogger _logger;

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
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _nameResolver = nameResolver ?? throw new ArgumentNullException(nameof(nameResolver));
            _serializerFactory = serializerFactory ?? ActivatorUtilities.CreateInstance<DefaultSerializerFactory>(serviceProvider);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClientFactory = httpClientFactory;
        }

        private void HandleErrors(HttpResponseMessage response, string requestUri)
        {
            // FIXME: implement
            response.EnsureSuccessStatusCode();
        }

        private TId ParseLocation<TData, TId>(string location, string requestUri)
        {
            if (location.StartsWith(_configuration.Endpoint))
            {
                var index = _configuration.Endpoint.Length;
                while (index < location.Length && location[index] == '/')
                {
                    ++index;
                }
                var typename = _nameResolver.ResolveTypeName(typeof(TData));
                if (location.AsSpan().Slice(index).StartsWith(typename))
                {
                    index += typename.Length;
                    while (index < location.Length && location[index] == '/')
                    {
                        ++index;
                    }
                    return (TId)Convert.ChangeType(location.Substring(index), typeof(TId));
                }
            }
            throw new RestException(requestUri, "REST CREATE returned invalid location.");
        }

        private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using var client = CreateClient();
            return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }

        protected virtual HttpClient CreateClient()
            => _httpClientFactory?.CreateClient(_configuration.HttpClient) ?? new HttpClient();

        public async Task<IReadOnlyList<T>> ListCollectionAsync<T>(
            string? target = default,
            string? filter = default,
            string? sortBy = default,
            string? sortByDirection = default,
            int offset = 0,
            int? limit = default,
            CancellationToken cancellationToken = default)
        {
            var requestUri = _configuration.GetCollectionEndpoint<T>(_nameResolver);
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Add("X-Filter", Uri.EscapeDataString(filter));
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
            await using var stream = await response.Content.ReadAsStreamAsync();
            return await _serializerFactory.DeserializeAsync<List<T>>(stream, cancellationToken);
        }

        public async Task<TData> ItemAsync<TData, TId>(
            TId id,
            CancellationToken cancellationToken = default)
            where TData : IHasId<TId>
        {
            var requestUri = _configuration.GetItemOrReductionEndpoint<TData>(_nameResolver, Convert.ToString(id, CultureInfo.InvariantCulture));
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            using var response = await SendAsync(request, cancellationToken);
            HandleErrors(response, requestUri);
            await using var stream = await response.Content.ReadAsStreamAsync();
            return await _serializerFactory.DeserializeAsync<TData>(stream, cancellationToken);
        }

        public async Task<TId> CreateAsync<TData, TId>(
            TData data,
            CancellationToken cancellationToken = default)
            where TData : IHasId<TId>
        {
            var requestUri = _configuration.GetCollectionEndpoint<TData>(_nameResolver);
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new SerializedContent<TData>(data, _serializerFactory.GetSerializer<TData>())
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

        public async Task UpdateAsync<TData, TId>(
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
            var requestUri = _configuration.GetCollectionEndpoint<TData>(_nameResolver);
            using var request = new HttpRequestMessage(HttpMethod.Put, requestUri)
            {
                Content = new SerializedContent<TData>(data, _serializerFactory.GetSerializer<TData>())
            };
            using var response = await SendAsync(request, cancellationToken);
            HandleErrors(response, requestUri);
        }

        public async Task DeleteAsync<TData, TId>(TId id, CancellationToken cancellationToken = default)
            where TData : IHasId<TId>
        {
            var requestUri = _configuration.GetItemOrReductionEndpoint<TData>(_nameResolver, Convert.ToString(id, CultureInfo.InvariantCulture));
            using var request = new HttpRequestMessage(HttpMethod.Delete, requestUri);
            using var response = await SendAsync(request, cancellationToken);
            HandleErrors(response, requestUri);
        }

        public async Task<object> ReductionAsync<T>(
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
            var requestUri = _configuration.GetItemOrReductionEndpoint<T>(_nameResolver, reduction);
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Add("X-Filter", Uri.EscapeDataString(filter));
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
            await using var stream = await response.Content.ReadAsStreamAsync();
            return await _serializerFactory.DeserializeAsync(stream, resultType, cancellationToken);
        }
    }
}