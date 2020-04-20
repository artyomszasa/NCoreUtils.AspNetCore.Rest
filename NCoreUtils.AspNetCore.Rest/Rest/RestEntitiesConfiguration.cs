using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace NCoreUtils.AspNetCore.Rest
{
    public class RestEntitiesConfiguration
    {
        readonly ImmutableDictionary<Type, string> _entityNames;

        readonly ImmutableDictionary<CaseInsensitive, Type> _entityTypes;

        internal RestEntitiesConfiguration(
            ImmutableDictionary<Type, string> entityNames,
            ImmutableDictionary<CaseInsensitive, Type> entityTypes)
        {
            _entityNames = entityNames ?? throw new ArgumentNullException(nameof(entityNames));
            _entityTypes = entityTypes ?? throw new ArgumentNullException(nameof(entityTypes));
        }

        public RestEntitiesConfiguration(IEnumerable<KeyValuePair<Type, string>> entries)
            : this(
                entries.ToImmutableDictionary(),
                entries.ToImmutableDictionary(e => CaseInsensitive.Create(e.Value), e => e.Key))
        { }


        public bool TryResolveType(CaseInsensitive name, out Type type)
            => _entityTypes.TryGetValue(name, out type);

        public bool TryGetName(Type type, out string name)
            => _entityNames.TryGetValue(type, out name);
    }
}