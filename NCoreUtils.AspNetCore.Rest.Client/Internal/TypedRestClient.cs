using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NCoreUtils.Data;
using NCoreUtils.Data.Protocol;
using NCoreUtils.Data.Protocol.Linq;
using NCoreUtils.Data.Protocol.Reductions;

namespace NCoreUtils.Rest.Internal;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(bool))]
internal partial class ReductionResultSerializerContext : JsonSerializerContext { }

public abstract class TypedRestClient(ILogger<TypedRestClient> logger)
    : IRestClient
{
    protected ILogger Logger { get; } = logger ?? throw new ArgumentNullException(nameof(logger));

    public abstract Type DataType { get; }

    public abstract Type IdType { get; }
}

public abstract class TypedRestClient<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TData>(
    ILogger<TypedRestClient<TData>> logger,
    IProtocolQueryProvider protocolQueryProvider)
    : TypedRestClient(logger)
    , IRestClient<TData>
{
    public override Type DataType => typeof(TData);

    public virtual IQueryable<TData> CreateQueryable() => DirectQuery.Create<TData>(protocolQueryProvider);

    public abstract IAsyncEnumerable<TData> ListCollectionAsync(
        string? target = default,
        string? filter = default,
        string? sortBy = default,
        string? sortByDirection = default,
        IReadOnlyList<string>? fields = default,
        IReadOnlyList<string>? includes = default,
        int offset = 0,
        int? limit = default,
        CancellationToken cancellationToken = default);

    public abstract Task<ReductionResult<TData>> ReductionAsync(
        Reduction reduction,
        string? target = null,
        string? filter = null,
        string? sortBy = null,
        string? sortByDirection = null,
        int offset = 0,
        int? limit = null,
        CancellationToken cancellationToken = default);
}

public class TypedRestClient<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TData, TId>(
    ILogger<TypedRestClient<TData, TId>> logger,
    IRestClientContext<TData, TId> context,
    IProtocolQueryProvider protocolQueryProvider)
    : TypedRestClient<TData>(logger, protocolQueryProvider)
    , IRestClient<TData, TId>
    where TData : class, IHasId<TId>
    where TId : IEquatable<TId>
{
    protected IRestClientContext<TData, TId> Context { get; } = context ?? throw new ArgumentNullException(nameof(context));

    public override Type IdType => typeof(TId);

    protected virtual void HandleErrors(HttpResponseMessage response, string requestUri)
    {
        ArgumentNullException.ThrowIfNull(response);
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

    protected TId ParseLocation(string location, string requestUri)
    {
        if (location.StartsWith(Context.Endpoint))
        {
            var index = Context.Endpoint.Length;
            while (index < location.Length && location[index] == '/')
            {
                ++index;
            }
            return Context.ParseId(location.AsSpan(index));
        }
        throw new RestException(requestUri, $"REST CREATE returned invalid location: {location}.");
    }

    protected async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var client = Context.CreateHttpClient();
        return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }

    public override async IAsyncEnumerable<TData> ListCollectionAsync(
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
        using var activity = G.ActivitySource.StartActivity("Remote REST COLLECTION method execution", System.Diagnostics.ActivityKind.Client);
        var requestUri = Context.GetCollectionEndpoint();
        Logger.LogRestCollectionUriResolved(typeof(TData), requestUri);
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        Context.QuerySerializer.Apply(request, target, filter, sortBy, sortByDirection, offset, limit);
        Logger.LogRestCollection(target, filter, sortBy, sortByDirection, fields, includes, offset, limit);
        using var response = await SendAsync(request, cancellationToken);
        HandleErrors(response, requestUri);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var subactivity = G.ActivitySource.StartActivity("Remote REST COLLECTION result deserialization");
        await foreach (var item in Context.DeserializeCollectionAsync(stream, cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }

    public virtual async Task<TData?> ItemAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        using var activity = G.ActivitySource.StartActivity("Remote REST ITEM method execution", System.Diagnostics.ActivityKind.Client);
        var requestUri = Context.GetItemEndpoint(id);
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        using var response = await SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }
        HandleErrors(response, requestUri);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var subactivity = G.ActivitySource.StartActivity("Remote REST ITEM result deserialization");
        return await Context.DeserializeItemAsync(stream, cancellationToken);
    }

    public virtual async Task<TId> CreateAsync(
        TData data,
        CancellationToken cancellationToken = default)
    {
        using var activity = G.ActivitySource.StartActivity("Remote REST CREATE method execution", System.Diagnostics.ActivityKind.Client);
        var requestUri = Context.GetCollectionEndpoint();
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new SerializedContent<TData>(data, Context.GetSerializer())
        };
        using var response = await SendAsync(request, cancellationToken);
        HandleErrors(response, requestUri);
        if (!response.Headers.TryGetValues("location", out var locationValues) || !locationValues.TryGetFirst(out var locationValue) || locationValue is null)
        {
            throw new RestException(requestUri, "REST CREATE returned no location.");
        }
        var id = ParseLocation(locationValue, requestUri);
        return id;
    }

    public virtual async Task UpdateAsync(
        TId id,
        TData data,
        CancellationToken cancellationToken = default)
    {
        if (!IdUtils.IsValidId(id))
        {
            throw new InvalidOperationException($"Invalid id.");
        }
        if (!id!.Equals(data.Id))
        {
            throw new InvalidOperationException($"Invalid id.");
        }
        using var activity = G.ActivitySource.StartActivity("Remote REST UPDATE method execution", System.Diagnostics.ActivityKind.Client);
        var requestUri = Context.GetItemEndpoint(data.Id);
        using var request = new HttpRequestMessage(HttpMethod.Put, requestUri)
        {
            Content = new SerializedContent<TData>(data, Context.GetSerializer())
        };
        using var response = await SendAsync(request, cancellationToken);
        HandleErrors(response, requestUri);
    }

    public virtual async Task DeleteAsync(TId id, bool force, CancellationToken cancellationToken = default)
    {
        var requestUri = Context.GetItemEndpoint(id);
        using var request = new HttpRequestMessage(HttpMethod.Delete, requestUri);
        if (force)
        {
            request.Headers.Add("X-Force", "true");
        }
        using var response = await SendAsync(request, cancellationToken);
        HandleErrors(response, requestUri);
    }

    public override async Task<ReductionResult<TData>> ReductionAsync(
        Reduction reduction,
        string? target = null,
        string? filter = null,
        string? sortBy = null,
        string? sortByDirection = null,
        int offset = 0,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var requestUri = Context.GetReductionEndpoint(reduction.Name);
        Logger.LogRestReductionUriResolved(typeof(TData), requestUri);
        using var activity = G.ActivitySource.StartActivity("Remote REST REDUCTION method execution", System.Diagnostics.ActivityKind.Client);
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        Context.QuerySerializer.Apply(request, target, filter, sortBy, sortByDirection, offset, limit);
        using var response = await SendAsync(request, cancellationToken);
        HandleErrors(response, requestUri);
        if (HttpStatusCode.NoContent == response.StatusCode)
        {
            return reduction switch
            {
                null => throw new ArgumentNullException(nameof(reduction)),
                First or
                Data.Protocol.Reductions.Single => ReductionResult<TData>.Item(default),
                Count => ReductionResult<TData>.Int32(default),
                Any => ReductionResult<TData>.Boolean(default),
                _ => throw new NotSupportedException($"Not supported reduction: {reduction}.")
            };
        }
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return reduction switch
        {
            null => throw new ArgumentNullException(nameof(reduction)),
            First or Data.Protocol.Reductions.Single => ReductionResult<TData>.Item(await DeserializerSingleItemAsync(Context, stream, cancellationToken)),
            Count => ReductionResult<TData>.Int32(await JsonSerializer.DeserializeAsync(stream, ReductionResultSerializerContext.Default.Int32, cancellationToken)),
            Any => ReductionResult<TData>.Boolean(await JsonSerializer.DeserializeAsync(stream, ReductionResultSerializerContext.Default.Boolean, cancellationToken)),
            _ => throw new NotSupportedException($"Not supported reduction: {reduction}.")
        };

        static async ValueTask<TData> DeserializerSingleItemAsync(IRestClientContext<TData, TId> context, Stream stream, CancellationToken cancellationToken)
        {
            using var subactivity = G.ActivitySource.StartActivity("Remote REST ITEM result deserialization");
            return await context.DeserializeItemAsync(stream, cancellationToken);
        }
    }
}