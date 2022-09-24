using Robust.Shared.Serialization;
using System;
using NetSerializer;
using Robust.Shared.Timing;

namespace Robust.Shared.GameObjects
{
    [Serializable, NetSerializable]
    public sealed class EntityState
    {
        public EntityUid Uid { get; }

        public NetListAsArray<ComponentChange> ComponentChanges { get; }

        public bool Empty => ComponentChanges.Value is null or { Count: 0 };

        public readonly GameTick EntityLastModified;

        public EntityState(EntityUid uid, NetListAsArray<ComponentChange> changedComponents, GameTick lastModified)
        {
            Uid = uid;
            ComponentChanges = changedComponents;
            EntityLastModified = lastModified;
        }
    }

    [Serializable, NetSerializable]
    public readonly struct ComponentChange
    {
        // 15ish bytes to create a component (strings are big), 5 bytes to remove one

        /// State data for the created/modified component, if any.
        /// </summary>
        public readonly ComponentState State;

        /// <summary>
        ///     The Network ID of the component to remove.
        /// </summary>
        public readonly ushort NetID;

        public readonly GameTick LastModifiedTick;

        public ComponentChange(ushort netId, ComponentState state, GameTick lastModifiedTick)
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
