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

                // Cast to handle nullability.
                return (EntityUid[]) _containedEntityArray!;
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
                _containedEntityArray[0] = value;
            }
        }

        public override List<EntityUid> ExpectedEntities => _expectedEntities;

        private EntityUid? _containedEntity;
        private readonly List<EntityUid> _expectedEntities = new();
        // Used by ContainedEntities to avoid allocating.
        private readonly EntityUid?[] _containedEntityArray = new EntityUid[1];

        /// <inheritdoc />
        public override string ContainerType => ClassName;

        /// <inheritdoc />
        public override bool CanInsert(EntityUid toinsert)
        {
            if (ContainedEntity != null)
                return false;
            return base.CanInsert(toinsert);
        }

        /// <inheritdoc />
        public override bool Contains(EntityUid contained)
        {
            if (contained == ContainedEntity)
                return true;
            return false;
        }

        /// <inheritdoc />
        protected override void InternalInsert(EntityUid toinsert)
        {
            ContainedEntity = toinsert;
            base.InternalInsert(toinsert);
        }

        /// <inheritdoc />
        protected override void InternalRemove(EntityUid toremove)
        {
            ContainedEntity = null;
            base.InternalRemove(toremove);
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            base.Shutdown();

            EntityUid? tempQualifier = ContainedEntity;
            if (tempQualifier != null)
            {
                IoCManager.Resolve<IEntityManager>().DeleteEntity(tempQualifier.Value);
            }
        }
    }
}
