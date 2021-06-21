using Robust.Shared.Serialization;
using System;

namespace Robust.Shared.GameObjects
{
    [Serializable, NetSerializable]
    public sealed class EntityState
    {
        public EntityUid Uid { get; }
        public ComponentChanged[]? ComponentChanges { get; }
        public ComponentState[]? ComponentStates { get; }

        public bool Empty => ComponentChanges is null && ComponentStates is null;

        public EntityState(EntityUid uid, ComponentChanged[]? changedComponents, ComponentState[]? componentStates)
        {
            Uid = uid;

            // empty lists are 5 bytes each
            ComponentChanges = changedComponents == null || changedComponents.Length == 0 ? null : changedComponents;
            ComponentStates = componentStates == null || componentStates.Length == 0 ? null : componentStates;
        }
    }

    [Serializable, NetSerializable]
    public readonly struct ComponentChanged
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

        //TODO: Add compstate field

        /// <summary>
        ///     The Network ID of the component to remove.
        /// </summary>
        public readonly uint NetID;

        public ComponentChanged(uint netId, bool created, bool deleted)
        {
            Deleted = deleted;
            NetID = netId;
            Created = created;
        }

        public override string ToString()
        {
            return $"{(Deleted ? "D" : "C")} {NetID}";
        }

        public static ComponentChanged Added(uint netId)
        {
            return new(netId, true, false);
        }

        public static ComponentChanged Removed(uint netId)
        {
            return new(netId, false, true);
        }
    }
}
