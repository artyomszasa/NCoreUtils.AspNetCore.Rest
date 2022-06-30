using System;
using System.Diagnostics.CodeAnalysis;
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
        ISerializer<T> GetSerializer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>();

        ValueTask SerializeAsync(
            IConfigurableOutput<Stream> configurableStream,
            object item,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type,
            CancellationToken cancellationToken = default);
    }
}