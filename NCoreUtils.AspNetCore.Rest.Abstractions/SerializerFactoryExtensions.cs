using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.AspNetCore.Rest;

public static class SerializerFactoryExtensions
{
    public static ValueTask SerializeAsync(
        this ISerializerFactory serializerFactory,
        IConfigurableOutput<Stream> configurableStream,
        object item,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type itemType,
        CancellationToken cancellationToken = default)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(item);
#else
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }
#endif
        return serializerFactory.SerializeAsync(configurableStream, item, itemType, cancellationToken);
    }
}