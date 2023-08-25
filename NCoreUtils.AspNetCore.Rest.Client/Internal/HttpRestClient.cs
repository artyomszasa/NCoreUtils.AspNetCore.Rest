using System;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
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
        [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Only used to create default values of value types.")]
        [UnconditionalSuppressMessage("Trimming", "IL2077", Justification = "Only used to create default values of value types.")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object CreateDefaultValue(Type type)
        {
            return (type.IsValueType ? Activator.CreateInstance(type) : null)!;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnconditionalSuppressMessage("Trimming", "IL2091", Justification = "Only called with non-asyncenumerable generic parameters.")]
        private ISerializer<T> GetNonAsyncEnumerableSerializer<T>()
            => SerializerFactory.GetSerializer<T>();

        protected IRestClientConfiguration Configuration { get; }

        protected IRestTypeNameResolver NameResolver { get; }

        protected ISerializerFactory SerializerFactory { get; }

        protected IRestQuerySerializer QuerySerializer { get; }

        protected ILogger Logger;

        private readonly IHttpClientFactory? _httpClientFactory;

#pragma warning disable CS0618
        public HttpRestClient(
            IServiceProvider serviceProvider,
            IRestClientConfiguration configuration,
            IRestTypeNameResolver nameResolver,
            ILogger<HttpRestClient> logger,
            ISerializerFactory? serializerFactory = default,
            IHttpClientFactory? httpClientFactory = default,
            IRestClientJsonTypeInfoResolver? restClientJsonTypeInfoResolver = default,
            IRestClientJsonSerializerContext? restClientJsonSerializerContext = default)
        {
            if (serviceProvider is null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            NameResolver = nameResolver ?? throw new ArgumentNullException(nameof(nameResolver));
            SerializerFactory = serializerFactory ?? restClientJsonTypeInfoResolver switch
            {
                null => restClientJsonSerializerContext switch
                {
                    null => throw new InvalidOperationException("Neither rest client type info resolver nor serializer factory has been registered."),
                    { JsonSerializerContext: var context } => new DefaultSerializerFactory(
                        logger: serviceProvider.GetRequiredService<ILogger<DefaultSerializerFactory>>(),
                        jsonSerializerContext: context
                    )
                },
                var resolver => new DefaultJsonSerializerFactory(
                    logger: serviceProvider.GetRequiredService<ILogger<DefaultJsonSerializerFactory>>(),
                    resolver: resolver
                )
            };
            QuerySerializer = serviceProvider.GetService<IRestQuerySerializer>() ?? RestQueryAsHeaderSerializer.Instance;
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClientFactory = httpClientFactory;
        }
#pragma warning restore CS0618

        protected virtual void HandleErrors(HttpResponseMessage response, string requestUri)
        {
            if (response is null)
            {
                throw new ArgumentNullException(nameof(response));
            }
            // check X-Message header
            if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.InternalServerError)
            {
                if (response.Headers is not null && response.Headers.TryGetValues("X-Message", out var values))
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

        [UnconditionalSuppressMessage("Trimming", "IL2091", Justification = "For compatibility reasons ISerializable argument must preserve interfaces, this is not the case here.")]
        public virtual async IAsyncEnumerable<T> ListCollectionAsync<T>(
            string? target = default,
            string? filter = default,
            string? sortBy = default,
            string? sortByDirection = default,
            IReadOnlyList<string>? fields = default,
            IReadOnlyList<string>? includes = default,
            int offset = 0,
            int? limit = default,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var requestUri = Configuration.GetCollectionEndpoint<T>(NameResolver);
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            QuerySerializer.Apply(request, target, filter, sortBy, sortByDirection, offset, limit);
            Logger.LogRestCollection(target, filter, sortBy, sortByDirection, fields, includes, offset, limit);
            using var response = await SendAsync(request, cancellationToken);
            HandleErrors(response, requestUri);
            await using var stream = await response.Content.ReadAsStreamAsync();
#if NET7_0_OR_GREATER
            await foreach (var item in SerializerFactory.DeserializeAsyncEnumerable<T>(stream, cancellationToken).ConfigureAwait(false))
            {
                yield return item;
            }
#else
            await foreach (var item in await SerializerFactory.DeserializeAsync<IAsyncEnumerable<T>>(stream, cancellationToken))
            {
                yield return item;
            }
#endif
        }

        public virtual async Task<TData?> ItemAsync<TData, TId>(
            TId id,
            CancellationToken cancellationToken = default)
            where TData : IHasId<TId>
        {
            var requestUri = Configuration.GetItemOrReductionEndpoint<TData>(NameResolver, Convert.ToString(id, CultureInfo.InvariantCulture)!);
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            using var response = await SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return default;
            }
            HandleErrors(response, requestUri);
            await using var stream = await response.Content.ReadAsStreamAsync();
            return await SerializerFactory.DeserializeNonAsyncEnumerableAsync<TData>(stream, cancellationToken);
        }

        public virtual async Task<TId> CreateAsync<TData, TId>(
            TData data,
            CancellationToken cancellationToken = default)
            where TData : IHasId<TId>
        {
            var requestUri = Configuration.GetCollectionEndpoint<TData>(NameResolver);
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new SerializedContent<TData>(data, GetNonAsyncEnumerableSerializer<TData>())
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
            var requestUri = Configuration.GetItemOrReductionEndpoint<TData>(NameResolver, Convert.ToString(data.Id, CultureInfo.InvariantCulture)!);
            using var request = new HttpRequestMessage(HttpMethod.Put, requestUri)
            {
                Content = new SerializedContent<TData>(data, GetNonAsyncEnumerableSerializer<TData>())
            };
            using var response = await SendAsync(request, cancellationToken);
            HandleErrors(response, requestUri);
        }

        public virtual async Task DeleteAsync<TData, TId>(TId id, bool force, CancellationToken cancellationToken = default)
            where TData : IHasId<TId>
        {
            var requestUri = Configuration.GetItemOrReductionEndpoint<TData>(NameResolver, Convert.ToString(id, CultureInfo.InvariantCulture)!);
            using var request = new HttpRequestMessage(HttpMethod.Delete, requestUri);
            if (force)
            {
                request.Headers.Add("X-Force", "true");
            }
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
                "any" => typeof(bool),
                _ => throw new NotSupportedException($"Not supported reduction: {reduction}.")
            };
            var requestUri = Configuration.GetItemOrReductionEndpoint<T>(NameResolver, reduction);
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            QuerySerializer.Apply(request, target, filter, sortBy, sortByDirection, offset, limit);
            using var response = await SendAsync(request, cancellationToken);
            HandleErrors(response, requestUri);
            if (HttpStatusCode.NoContent == response.StatusCode)
            {
                return CreateDefaultValue(resultType);
            }
            await using var stream = await response.Content.ReadAsStreamAsync();
            return await SerializerFactory.DeserializeAsync(stream, resultType, cancellationToken);
        }
    }
}