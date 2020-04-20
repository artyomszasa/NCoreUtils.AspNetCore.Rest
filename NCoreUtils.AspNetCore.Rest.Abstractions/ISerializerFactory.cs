using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.AspNetCore.Rest
{
    /// <summary>
    /// Defines functionality to retrieve typed serializer when the exact type is not known at the instantiation time,
    /// e.g. when performing a reduction.
    /// </summary>
    public interface ISerializerFactory
    {
        ISerializer<T> GetSerializer<T>();

        ValueTask SerializeAsync(
            IConfigurableOutput<Stream> configurableStream,
            object item,
            Type type,
            CancellationToken cancellationToken = default);
    }
}