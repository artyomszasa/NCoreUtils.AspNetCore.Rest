using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Rest
{
    public interface ISerializer<T>
    {
        string? ContentType { get; }

        ValueTask<T> DeserializeAsync(Stream stream, CancellationToken cancellationToken = default);

#if NET7_0_OR_GREATER
        IAsyncEnumerable<T> DeserializeAsyncEnumerable(Stream stream, CancellationToken cancellationToken = default);
#endif

        ValueTask SerializeAsync(Stream stream, T value, CancellationToken cancellationToken = default);
    }
}