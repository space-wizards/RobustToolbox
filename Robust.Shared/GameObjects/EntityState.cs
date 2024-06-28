using Robust.Shared.Serialization;
using System;
using NetSerializer;
using Robust.Shared.Timing;
using System.Collections.Generic;

namespace Robust.Shared.GameObjects
{
    [Serializable, NetSerializable]
    public sealed class EntityState
    {
        /// <summary>
        /// Network identifier for the entity.
        /// </summary>
        public NetEntity NetEntity;

        public NetListAsArray<ComponentChange> ComponentChanges { get; }

        public bool Empty => (ComponentChanges.Value is null or { Count: 0 }) && NetComponents == null;

        public readonly GameTick EntityLastModified;

        /// <summary>
        ///     Set of all networked component ids. Only sent to clients if a component has been removed sometime since the
        ///     entity was last sent to a player.
        /// </summary>
        public HashSet<ushort>? NetComponents;

        public EntityState(NetEntity netEntity, NetListAsArray<ComponentChange> changedComponents, GameTick lastModified, HashSet<ushort>? netComps = null)
        {
            NetEntity = netEntity;
            ComponentChanges = changedComponents;
            EntityLastModified = lastModified;
            NetComponents = netComps;
        }
    }

    [Serializable, NetSerializable]
    public readonly struct ComponentChange
    {
        /// <summary>
        /// State data for the created/modified component, if any.
        /// </summary>
        public readonly IComponentState? State;

        /// <summary>
        ///     The Network ID of the component to remove.
        /// </summary>
        public readonly ushort NetID;

        public readonly GameTick LastModifiedTick;

        public ComponentChange(ushort netId, IComponentState? state, GameTick lastModifiedTick)
        {
            State = state;
            NetID = netId;
            LastModifiedTick = lastModifiedTick;
        }

        public override string ToString()
        {
            return $"{NetID} {State?.GetType().Name}";
        }
    }
}
