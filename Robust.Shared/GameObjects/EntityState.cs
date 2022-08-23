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

        /// <summary>
        ///     Was the component removed from the entity.
        /// </summary>
        public readonly bool Deleted;

        /// <summary>
        /// Was the component added to the entity.
        /// </summary>
        public readonly bool Created;

        /// <summary>
        /// State data for the created/modified component, if any.
        /// </summary>
        public readonly ComponentState? State;

        /// <summary>
        ///     The Network ID of the component to remove.
        /// </summary>
        public readonly ushort NetID;

        public readonly GameTick LastModifiedTick;

        public ComponentChange(ushort netId, bool created, bool deleted, ComponentState? state, GameTick lastModifiedTick)
        {
            Deleted = deleted;
            State = state;
            NetID = netId;
            Created = created;
            LastModifiedTick = lastModifiedTick;
        }

        public override string ToString()
        {
            return $"{(Deleted ? "D" : "C")} {NetID} {State?.GetType().Name}";
        }

        public static ComponentChange Added(ushort netId, ComponentState? state, GameTick lastModifiedTick)
        {
            return new(netId, true, false, state, lastModifiedTick);
        }

        public static ComponentChange Changed(ushort netId, ComponentState state, GameTick lastModifiedTick)
        {
            return new(netId, false, false, state, lastModifiedTick);
        }

        public static ComponentChange Removed(ushort netId)
        {
            return new(netId, false, true, null, default);
        }
    }
}
