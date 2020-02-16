using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;

namespace Robust.Shared.GameObjects
{
    [Serializable, NetSerializable]
    public sealed class EntityState
    {
        public EntityUid Uid { get; }
        public List<ComponentChanged> ComponentChanges { get; }
        public List<ComponentState> ComponentStates { get; }

        public EntityState(EntityUid uid, List<ComponentChanged> changedComponents, List<ComponentState> componentStates)
        {
            Uid = uid;

            // empty lists are 5 bytes each
            ComponentChanges = changedComponents == null || changedComponents.Count == 0 ? null : changedComponents;
            ComponentStates = componentStates == null || componentStates.Count == 0 ? null : componentStates;
        }
    }

    [Serializable, NetSerializable]
    public readonly struct ComponentChanged
    {
        // 15ish bytes to create a component (strings are big), 5 bytes to remove one

        /// <summary>
        ///     Was the component added or removed from the entity.
        /// </summary>
        public readonly bool Deleted;

        /// <summary>
        ///     The Network ID of the component to remove.
        /// </summary>
        public readonly uint NetID;

        /// <summary>
        ///     The prototype name of the component to add.
        /// </summary>
        public readonly string ComponentName;

        public ComponentChanged(bool deleted, uint netId, string componentName)
        {
            Deleted = deleted;
            NetID = netId;
            ComponentName = componentName;
        }

        public override string ToString()
        {
            return $"{(Deleted ? "D" : "C")} {NetID} {ComponentName}";
        }

        public static ComponentChanged Added(uint netId, string componentName)
        {
            return new ComponentChanged(false, netId, componentName);
        }

        public static ComponentChanged Removed(uint netId)
        {
            return new ComponentChanged(true, netId, null);
        }
    }
}
