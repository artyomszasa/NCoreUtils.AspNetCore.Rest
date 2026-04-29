using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Runtime.CompilerServices;

namespace NCoreUtils.Rest.Internal;

public class RestClientContext<TData, TId>(
    IHttpClientFactory httpClientFactory,
    ISerializer<TData> serializer,
    IRestQuerySerializer querySerializer,
    IRestIdHandler<TId> idHandler,
    IReadOnlyList<IRestClientErrorHandler> errorHandlers,
    string endpoint,
    string httpClientConfigurationName)
    : IRestClientContext<TData, TId>
{
    protected IHttpClientFactory HttpClientFactory { get; } = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

#pragma warning disable CA1721 // Property names should not match get methods
    protected ISerializer<TData> Serializer { get; } = serializer ?? throw new ArgumentNullException(nameof(serializer));
#pragma warning restore CA1721 // Property names should not match get methods

    protected IRestIdHandler<TId> IdHandler { get; } = idHandler ?? throw new ArgumentNullException(nameof(idHandler));

    [SuppressMessage("Style", "IDE0301:Simplify collection initialization", Justification = "Possible foreach optimization.")]
    public IReadOnlyList<IRestClientErrorHandler> ErrorHandlers { get; } = errorHandlers ?? Array.Empty<IRestClientErrorHandler>();

    public IRestQuerySerializer QuerySerializer { get; } = querySerializer ?? throw new ArgumentNullException(nameof(querySerializer));

    public string Endpoint { get; } = endpoint ?? throw new ArgumentNullException(nameof(endpoint));

    public string HttpClientConfigurationName { get; } = httpClientConfigurationName ?? throw new ArgumentNullException(nameof(httpClientConfigurationName));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HttpClient CreateHttpClient()
        => HttpClientFactory.CreateClient(HttpClientConfigurationName);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ISerializer<TData> GetSerializer()
        => Serializer;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TId ParseId(ReadOnlySpan<char> input)
        => IdHandler.ParseId(input);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryStringifyId(TId id, Span<char> buffer, out int used)
        => IdHandler.TryStringifyId(id, buffer, out used);
}