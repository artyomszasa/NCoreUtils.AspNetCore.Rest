namespace NCoreUtils.Rest;

public interface ISerializer<T>
{
    string? ContentType { get; }

    ValueTask<T> DeserializeAsync(Stream stream, CancellationToken cancellationToken = default);

    IAsyncEnumerable<T> DeserializeAsyncEnumerable(Stream stream, CancellationToken cancellationToken = default);

    ValueTask SerializeAsync(Stream stream, T value, CancellationToken cancellationToken = default);
}