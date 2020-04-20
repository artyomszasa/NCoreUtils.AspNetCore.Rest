using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.AspNetCore.Rest
{
    /// <summary>
    /// Defines functionality for serializing objects of concrete.
    /// </summary>
    /// <typeparam name="T">Type of the object to serialize.</typeparam>
    public interface ISerializer<T>
    {
        /// <summary>
        /// Serializes object of the predefined type to configurable output.
        /// </summary>
        /// <param name="configurableStream">Configurable stream output to serialize to.</param>
        /// <param name="item">Object to serialize to.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        ValueTask SerializeAsync(IConfigurableOutput<Stream> configurableStream, T item, CancellationToken cancellationToken = default);
    }
}