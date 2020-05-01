using System;

namespace NCoreUtils.Rest.Internal
{
    public sealed class DefaultRestTypeNameResolver : IRestTypeNameResolver
    {
        public string ResolveTypeName(Type type)
            => type.Name;
    }
}