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
        Preconditions.ThrowIfNull(serializerFactory);
        return serializerFactory.SerializeAsync(configurableStream, item, itemType, cancellationToken);
    }
}