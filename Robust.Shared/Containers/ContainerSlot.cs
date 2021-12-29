using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Containers
{
    [UsedImplicitly]
    [SerializedType(ClassName)]
    public class ContainerSlot : BaseContainer
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

        [ViewVariables]
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
            if (ContainedEntity != null)
                return false;
            return base.CanInsert(toinsert, entMan);
        }

        /// <summary>
        ///     Performs the same check as <see cref="CanInsert"/> without requiring that the slot has to be empty.
        /// </summary>
        /// <remarks>
        ///     Useful if you need to know whether an item could be inserted into a slot, without having to actually
        ///     eject the currently contained entity first.
        /// </remarks>
        public bool CanInsertIfEmpty(EntityUid toinsert, IEntityManager? entMan = null)
        {
            return base.CanInsert(toinsert, entMan);
        }

        /// <inheritdoc />
        public override bool Contains(EntityUid contained)
        {
            if (contained == ContainedEntity)
                return true;
            return false;
        }

        /// <inheritdoc />
        protected override void InternalInsert(EntityUid toinsert, IEntityManager? entMan = null)
        {
            ContainedEntity = toinsert;
            base.InternalInsert(toinsert, entMan);
        }

        /// <inheritdoc />
        protected override void InternalRemove(EntityUid toremove, IEntityManager? entMan = null)
        {
            ContainedEntity = null;
            base.InternalRemove(toremove, entMan);
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            base.Shutdown();

            if (ContainedEntity is {} contained)
            {
                IoCManager.Resolve<IEntityManager>().DeleteEntity(contained);
            }
        }
    }
}
