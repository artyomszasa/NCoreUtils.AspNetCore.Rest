namespace NCoreUtils.Rest
{
    public interface ISerializerFactory
    {
        string? ContentType { get; }

        ISerializer<T> GetSerializer<T>();
    }
}