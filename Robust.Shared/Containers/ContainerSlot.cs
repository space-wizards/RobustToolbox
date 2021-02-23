using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Containers
{
    [UsedImplicitly]
    [SerializedType(ClassName)]
    public class ContainerSlot : BaseContainer
    {
        private const string ClassName = "ContainerSlot";

        private IEntity? _containedEntity;

        /// <inheritdoc />
        public override IReadOnlyList<IEntity> ContainedEntities
        {
            get
            {
                if (ContainedEntity == null) return Array.Empty<IEntity>();

                return new List<IEntity> {ContainedEntity};
            }
        }

        [ViewVariables]
        public IEntity? ContainedEntity
        {
            get => _containedEntity;
            private set => _containedEntity = value;
        }

        /// <inheritdoc />
        public override string ContainerType => ClassName;
        
        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

#if SERV3
            // ONLY PAUL CAN MAKE ME WHOLE
            serializer.DataField(ref _containedEntity, "ent", default);
#else
            if (serializer.Writing)
            {
                serializer.DataWriteFunction("ents", EntityUid.Invalid,
                    () => _containedEntity?.Uid ?? EntityUid.Invalid);
            }
            else
            {
                var entMan = IoCManager.Resolve<IEntityManager>();

                serializer.DataReadFunction("ent", EntityUid.Invalid,
                    value => _containedEntity = value != EntityUid.Invalid ? entMan.GetEntity(value) : null);
            }
#endif
        }

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
