using Robust.Shared.Serialization;
using System;

namespace Robust.Shared.GameObjects
{
    [Serializable, NetSerializable]
    public sealed class EntityState
    {
        public EntityUid Uid { get; }

        public ComponentChange[]? ComponentChanges { get; }

        public bool Empty => ComponentChanges is null;

        public EntityState(EntityUid uid, ComponentChange[]? changedComponents)
        {
            Uid = uid;

            // empty lists are 5 bytes each
            ComponentChanges = changedComponents == null || changedComponents.Length == 0 ? null : changedComponents;
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


        public readonly ComponentState? State;

        /// <summary>
        ///     The Network ID of the component to remove.
        /// </summary>
        public readonly uint NetID;

        public ComponentChange(uint netId, bool created, bool deleted, ComponentState? state)
        {
            Deleted = deleted;
            State = state;
            NetID = netId;
            Created = created;
        }

        public override string ToString()
        {
            return $"{(Deleted ? "D" : "C")} {NetID} {State?.GetType().Name}";
        }

        public static ComponentChange Added(uint netId, ComponentState? state)
        {
            return new(netId, true, false, state);
        }

        public static ComponentChange Changed(uint netId, ComponentState state)
        {
            return new(netId, false, false, state);
        }

        public static ComponentChange Removed(uint netId)
        {
            return new(netId, false, true, null);
        }
    }
}
