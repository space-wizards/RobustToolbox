using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Robust.Shared.Containers
{
    [UsedImplicitly]
    [SerializedType(ClassName)]
    public sealed class ContainerSlot : BaseContainer
    {
        private const string ClassName = "ContainerSlot";

        /// <inheritdoc />
        public override IReadOnlyList<EntityUid> ContainedEntities
        {
            get
            {
                if (ContainedEntity == null)
                    return Array.Empty<EntityUid>();

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
                    _containedEntityArray[0] = value!.Value;
            }
        }

        public override List<EntityUid> ExpectedEntities => _expectedEntities;

        private EntityUid? _containedEntity;
        private readonly List<EntityUid> _expectedEntities = new();
        // Used by ContainedEntities to avoid allocating.
        private readonly EntityUid[] _containedEntityArray = new EntityUid[1];

        /// <inheritdoc />
        public override string ContainerType => ClassName;

        /// <inheritdoc />
        public override bool CanInsert(EntityUid toinsert, IEntityManager? entMan = null)
        {
            return (ContainedEntity == null) && CanInsertIfEmpty(toinsert, entMan);
        }

        /// <summary>
        /// Checks if the entity can be inserted into this container, assuming that the container slot is empty.
        /// </summary>
        /// <remarks>
        /// Useful if you need to know whether an item could be inserted into a slot, without having to actually eject
        /// the currently contained entity first.
        /// </remarks>
        /// <param name="toinsert">The entity to attempt to insert.</param>
        /// <returns>True if the entity could be inserted into an empty slot, false otherwise.</returns>
        public bool CanInsertIfEmpty(EntityUid toinsert, IEntityManager? entMan = null)
        {
            return base.CanInsert(toinsert, entMan);
        }

        /// <inheritdoc />
        public override bool Contains(EntityUid contained)
        {
            if (contained != ContainedEntity)
                return false;


            var flags = IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(contained).Flags;
            DebugTools.Assert((flags & MetaDataFlags.InContainer) != 0);

            return true;
        }

        /// <inheritdoc />
        protected override void InternalInsert(EntityUid toInsert, IEntityManager entMan)
        {
            DebugTools.Assert(ContainedEntity == null);
            ContainedEntity = toInsert;
        }

        /// <inheritdoc />
        protected override void InternalRemove(EntityUid toRemove, IEntityManager entMan)
        {
            DebugTools.Assert(ContainedEntity == toRemove);
            ContainedEntity = null;
        }

        /// <inheritdoc />
        protected override void InternalShutdown(IEntityManager entMan, bool isClient)
        {
            if (ContainedEntity is not { } entity)
                return;

            if (!isClient)
                entMan.DeleteEntity(entity);
            else if (entMan.EntityExists(entity))
                Remove(entity, entMan, reparent: false, force: true);
        }
    }
}
