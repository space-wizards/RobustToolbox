using System;
using System.Collections.Generic;
using SS14.Server.ViewVariables.Traits;
using SS14.Shared.Network;
using SS14.Shared.ViewVariables;

namespace SS14.Server.ViewVariables
{
    internal sealed class ViewVariablesSession
    {
        private readonly List<ViewVariablesTrait> _traits = new List<ViewVariablesTrait>();
        public IViewVariablesHost Host { get; }
        public NetSessionId PlayerSession { get; }
        public object Object { get; }
        public uint SessionId { get; }
        public Type ObjectType { get; }

        /// <param name="playerSession">The session ID of the player who opened this session.</param>
        /// <param name="o">The object we represent.</param>
        /// <param name="sessionId">
        ///     The session ID for this session. This is what the server and client use to talk about this session.
        /// </param>
        /// <param name="host">The view variables host owning this session.</param>
        public ViewVariablesSession(NetSessionId playerSession, object o, uint sessionId, IViewVariablesHost host)
        {
            PlayerSession = playerSession;
            Object = o;
            SessionId = sessionId;
            ObjectType = o.GetType();
            Host = host;

            var traitIds = Host.TraitIdsFor(ObjectType);
            if (traitIds.Contains(ViewVariablesTraits.Members))
            {
                var trait = new ViewVariablesTraitMembers(this);
                _traits.Add(trait);
            }

            if (traitIds.Contains(ViewVariablesTraits.Enumerable))
            {
                var trait = new ViewVariablesTraitEnumerable(this);
                _traits.Add(trait);
            }

            if (traitIds.Contains(ViewVariablesTraits.Entity))
            {
                var trait = new ViewVariablesTraitEntity(this);
                _traits.Add(trait);
            }
        }

        public ViewVariablesBlob DataRequest(ViewVariablesRequest messageRequestMeta)
        {
            if (messageRequestMeta is ViewVariablesRequestMetadata)
            {
                return new ViewVariablesBlobMetadata
                {
                    ObjectType = ObjectType.AssemblyQualifiedName,
                    ObjectTypePretty = ObjectType.ToString(),
                    Stringified = Object.ToString(),
                    Traits = new List<object>(Host.TraitIdsFor(ObjectType))
                };
            }

            foreach (var trait in _traits)
            {
                var blob = trait.DataRequest(messageRequestMeta);
                if (blob != null)
                {
                    return blob;
                }
            }

            return null;
        }

        public void Modify(object[] propertyIndex, object value)
        {
            foreach (var trait in _traits)
            {
                if (trait.TryModifyProperty(propertyIndex, value))
                {
                    break;
                }
            }
        }

        public bool TryGetRelativeObject(object[] propertyIndex, out object value)
        {
            foreach (var trait in _traits)
            {
                if (trait.TryGetRelativeObject(propertyIndex[0], out value))
                {
                    return true;
                }
            }

            value = default;
            return false;
        }
    }
}
