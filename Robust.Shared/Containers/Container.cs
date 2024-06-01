using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Robust.Shared.Containers
{
    /// <summary>
    /// Default implementation for containers,
    /// cannot be inherited. If additional logic is needed,
    /// this logic should go on the systems that are holding this container.
    /// For example, inventory containers should be modified only through an inventory component.
    /// </summary>
    [UsedImplicitly]
    [SerializedType(nameof(Container))]
    public sealed partial class Container : BaseContainer
    {
        /// <summary>
        /// The generic container class uses a list of entities
        /// </summary>
        [DataField("ents")]
        [NonSerialized]
        private List<EntityUid> _containerList = new();

        public override int Count => _containerList.Count;

        /// <inheritdoc />
        public override IReadOnlyList<EntityUid> ContainedEntities => _containerList;

        /// <inheritdoc />
        protected internal override void InternalInsert(EntityUid toInsert, IEntityManager entMan)
        {
            DebugTools.Assert(!_containerList.Contains(toInsert));
            _containerList.Add(toInsert);
        }

        /// <inheritdoc />
        protected internal override void InternalRemove(EntityUid toRemove, IEntityManager entMan)
        {
            _containerList.Remove(toRemove);
        }

        /// <inheritdoc />
        public override bool Contains(EntityUid contained)
        {
            if (!_containerList.Contains(contained))
                return false;

            System?.AssertInContainer(contained, this);
            return true;
        }

        /// <inheritdoc />
        protected internal override void InternalShutdown(IEntityManager entMan, SharedContainerSystem system, bool isClient)
        {
            foreach (var entity in _containerList.ToArray())
            {
                if (!isClient)
                    entMan.DeleteEntity(entity);
                else if (entMan.EntityExists(entity))
                    system.Remove(entity, this, reparent: false, force: true);
            }
        }
    }
}
