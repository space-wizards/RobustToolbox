using System;
using System.Collections;
using System.Collections.Generic;
using Robust.Shared.GameObjects;

namespace Robust.Shared.ViewVariables
{
    internal class ViewVariablesManagerShared
    {
        private readonly Dictionary<Type, HashSet<object>> _cachedTraits = new();

        /// <summary>
        ///     Figures out which VV traits an object type has. This method is in shared so the client and server agree on this mess.
        /// </summary>
        /// <seealso cref="ViewVariablesBlobMetadata.Traits"/>
        public ICollection<object> TraitIdsFor(Type type)
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
