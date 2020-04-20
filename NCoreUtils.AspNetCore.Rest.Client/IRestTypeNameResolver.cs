using System;

namespace NCoreUtils.Rest
{
    public interface IRestTypeNameResolver
    {
        string ResolveTypeName(Type type);
    }
}