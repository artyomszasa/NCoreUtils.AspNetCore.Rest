using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.AspNetCore.Rest
{
    /// <summary>
    /// Defines functionality for deserializing objects of concrete type.
    /// </summary>
    /// <typeparam name="a">Type of the object to deserialize.</typeparam>
    public interface IDeserializer<T>
    {
        /// <summary>
        /// Deserializes object of the predefined type from stream.
        /// </summary>
        /// <param name="stream">Stream to deserialize object from.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Deserialized object.</returns>
        ValueTask<T> DeserializeAsync(Stream stream, CancellationToken cancellationToken);
    }
}