using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
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
        public override IReadOnlyList<IEntity> ContainedEntities
        {
            get
            {
                if (ContainedEntity == null)
                    return Array.Empty<IEntity>();

                // Cast to handle nullability.
                return (IEntity[]) _containedEntityArray!;
            }
        }

        [ViewVariables]
        [DataField("ent")]
        public IEntity? ContainedEntity
        {
            get => _containedEntity;
            private set
            {
                _containedEntity = value;
                _containedEntityArray[0] = value;
            }
        }

        private IEntity? _containedEntity;
        // Used by ContainedEntities to avoid allocating.
        private readonly IEntity?[] _containedEntityArray = new IEntity[1];

        /// <inheritdoc />
        public override string ContainerType => ClassName;

        /// <inheritdoc />
        public override bool CanInsert(IEntity toinsert)
        {
            if (ContainedEntity != null)
                return false;
            return base.CanInsert(toinsert);
        }

        /// <inheritdoc />
        public override bool Contains(IEntity contained)
        {
            if (contained == ContainedEntity)
                return true;
            return false;
        }

        /// <inheritdoc />
        protected override void InternalInsert(IEntity toinsert)
        {
            ContainedEntity = toinsert;
            base.InternalInsert(toinsert);
        }

        /// <inheritdoc />
        protected override void InternalRemove(IEntity toremove)
        {
            ContainedEntity = null;
            base.InternalRemove(toremove);
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            base.Shutdown();

            ContainedEntity?.Delete();
        }
    }
}
