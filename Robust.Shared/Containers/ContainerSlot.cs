using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Robust.Shared.Containers
{
    [UsedImplicitly]
    [SerializedType(nameof(ContainerSlot))]
    public sealed partial class ContainerSlot : BaseContainer
    {
        public override int Count => ContainedEntity == null ? 0 : 1;

        /// <inheritdoc />
        public override IReadOnlyList<EntityUid> ContainedEntities
        {
            get
            {
                if (_containedEntity == null)
                    return Array.Empty<EntityUid>();

                _containedEntityArray ??= new[] { _containedEntity.Value };
                DebugTools.Assert(_containedEntityArray[0] == _containedEntity);
                return _containedEntityArray;
            }
        }

        [DataField("ent")]
        public EntityUid? ContainedEntity
        {
            get => _containedEntity;
            private set
            {
                _containedEntity = value;
                if (value != null)
                {
                    _containedEntityArray ??= new EntityUid[1];
                    _containedEntityArray[0] = value.Value;
                }
            }
        }

        [NonSerialized]
        private EntityUid? _containedEntity;

        // Used by ContainedEntities to avoid allocating.
        [NonSerialized]
        private EntityUid[]? _containedEntityArray;

        /// <inheritdoc />
        public override bool Contains(EntityUid contained)
        {
            if (contained != ContainedEntity)
                return false;

            System?.AssertInContainer(contained, this);
            return true;
        }

        protected internal override bool CanInsert(EntityUid toInsert, bool assumeEmpty, IEntityManager entMan)
            => ContainedEntity == null || assumeEmpty;

        /// <inheritdoc />
        protected internal override void InternalInsert(EntityUid toInsert, IEntityManager entMan)
        {
            DebugTools.Assert(ContainedEntity == null);

            #if DEBUG
            // TODO make this a proper debug assert when gun code no longer fudges client-side spawn prediction.
            if (entMan.IsClientSide(toInsert) && !entMan.IsClientSide(Owner) && Manager.NetSyncEnabled && !entMan.HasComponent<PredictedSpawnComponent>(toInsert))
                Logger.Warning("Inserting a client-side entity into a networked container slot. This will block the container slot and may cause issues.");
            #endif
            ContainedEntity = toInsert;
        }

        /// <inheritdoc />
        protected internal override void InternalRemove(EntityUid toRemove, IEntityManager entMan)
        {
            DebugTools.Assert(ContainedEntity == toRemove);
            ContainedEntity = null;
        }

        /// <inheritdoc />
        protected internal override void InternalShutdown(IEntityManager entMan, SharedContainerSystem system, bool isClient)
        {
            if (ContainedEntity is not { } entity)
                return;

            if (!isClient)
                entMan.DeleteEntity(entity);
            else if (entMan.EntityExists(entity))
                system.Remove(entity, this, reparent: false, force: true);
        }
    }
}
