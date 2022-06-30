using System.Diagnostics.CodeAnalysis;

namespace NCoreUtils.Rest
{
    public interface ISerializerFactory
    {
        string? ContentType { get; }

        ISerializer<T> GetSerializer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>();
    }
}