using System;
using System.Collections;
using System.Collections.Generic;
using SS14.Shared.Interfaces.GameObjects;

namespace SS14.Shared.ViewVariables
{
    internal class ViewVariablesManagerShared
    {
        private readonly Dictionary<Type, HashSet<object>> _cachedTraits = new Dictionary<Type, HashSet<object>>();

        protected HashSet<object> TraitIdsFor(Type type)
        {
            if (!_cachedTraits.TryGetValue(type, out var traits))
            {
                traits = new HashSet<object>();
                _cachedTraits.Add(type, traits);
                if (ViewVariablesUtility.TypeHasVisibleMembers(type))
                {
                    traits.Add(ViewVariablesTraits.Members);
                }

                if (typeof(IEnumerable).IsAssignableFrom(type))
                {
                    traits.Add(ViewVariablesTraits.Enumerable);
                }

                if (typeof(IEntity).IsAssignableFrom(type))
                {
                    traits.Add(ViewVariablesTraits.Entity);
                }
            }

            return traits;
        }
    }
}
