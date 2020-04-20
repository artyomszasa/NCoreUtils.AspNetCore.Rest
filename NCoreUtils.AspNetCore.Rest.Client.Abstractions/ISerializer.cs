using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Rest
{
    public interface ISerializer<T>
    {
        string? ContentType { get; }

        ValueTask<T> DeserializeAsync(Stream stream, CancellationToken cancellationToken = default);

        ValueTask SerializeAsync(Stream stream, T value, CancellationToken cancellationToken = default);
    }
}