using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Rest;

/// <summary>
/// Common interface for REST client contexts that wraps functionality required to perform operations for the single
/// entity type.
/// </summary>
public interface IRestClientContext
{
    string Endpoint { get; }

    IRestQuerySerializer QuerySerializer { get; }

    IReadOnlyList<IRestClientErrorHandler> ErrorHandlers { get; }

    HttpClient CreateHttpClient();

    string GetCollectionEndpoint()
        => Endpoint;
}

/// <summary>
/// Wraps required functionality to perform operations for the single entity type.
/// </summary>
/// <typeparam name="TData">Entity type.</typeparam>
/// <typeparam name="TId">Id type of the entity.</typeparam>
public interface IRestClientContext<TData, TId> : IRestClientContext
{
    string GetItemEndpoint(TId id)
    {
        var buffer = ArrayPool<char>.Shared.Rent(16 * 1024);
        try
        {
            var bufferSpan = buffer.AsSpan();
            var prefix = Endpoint;
            prefix.CopyTo(bufferSpan);
            var offset = prefix.Length;
            if (prefix.Length == 0 || prefix[^1] != '/')
            {
                bufferSpan[offset++] = '/';
            }
            offset += StringifyId(id, bufferSpan[offset..]);
            return new(bufferSpan[.. offset]);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer, clearArray: false);
        }
    }

    string GetReductionEndpoint(string reduction)
    {
        var buffer = ArrayPool<char>.Shared.Rent(16 * 1024);
        try
        {
            var bufferSpan = buffer.AsSpan();
            var prefix = Endpoint;
            prefix.CopyTo(bufferSpan);
            var offset = prefix.Length;
            if (prefix.Length == 0 || prefix[^1] != '/')
            {
                bufferSpan[offset++] = '/';
            }
            reduction.ThrowIfNull().CopyTo(bufferSpan[offset..]);
            offset += reduction.Length;
            return new(bufferSpan[.. offset]);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer, clearArray: false);
        }
    }

    ISerializer<TData> GetSerializer();

    TId ParseId(ReadOnlySpan<char> input);

    int StringifyId(TId id, Span<char> buffer)
        => TryStringifyId(id, buffer, out var used)
            ? used
            :  throw new InvalidOperationException("Insufficient buffer to stringify id to.");

    bool TryStringifyId(TId id, Span<char> buffer, out int used);

    ValueTask<TData> DeserializeItemAsync(Stream stream, CancellationToken cancellationToken = default)
        => GetSerializer().DeserializeAsync(stream, cancellationToken);

    IAsyncEnumerable<TData> DeserializeCollectionAsync(Stream stream, CancellationToken cancellationToken = default)
        => GetSerializer().DeserializeAsyncEnumerable(stream, cancellationToken);
}