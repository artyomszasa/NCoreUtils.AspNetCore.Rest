using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace NCoreUtils.AspNetCore.Rest
{
    public class RestEntitiesConfigurationBuilder
    {
        readonly Dictionary<Type, string> _entityNames = new Dictionary<Type, string>();

        readonly Dictionary<CaseInsensitive, Type> _entityTypes = new Dictionary<CaseInsensitive, Type>();

        public RestEntitiesConfigurationBuilder Add(Type type, CaseInsensitive name)
        {
            if (_entityNames.ContainsKey(type))
            {
                throw new InvalidOperationException($"{type} has already been registered.");
            }
            if (_entityTypes.TryGetValue(name, out var xtype))
            {
                throw new InvalidOperationException($"{xtype} has already been registered with name = {name}.");
            }
            _entityNames.Add(type, name.ToLowerString());
            _entityTypes.Add(name, type);
            return this;
        }

        public RestEntitiesConfigurationBuilder Add(Type type)
            => Add(type, type.Name.ToLowerInvariant());

        public RestEntitiesConfigurationBuilder Add<T>(CaseInsensitive name)
            => Add(typeof(T), name);

        public RestEntitiesConfigurationBuilder Add<T>()
            => Add(typeof(T));

        public RestEntitiesConfigurationBuilder AddRange(params Type[] types)
        {
            foreach (var type in types)
            {
                Add(type);
            }
            return this;
        }

        public RestEntitiesConfiguration Build()
            => new RestEntitiesConfiguration(
                _entityNames.ToImmutableDictionary(),
                _entityTypes.ToImmutableDictionary()
            );
    }
}