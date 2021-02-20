using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Containers
{
    [SerializedType(ClassName)]
    public class ContainerSlot : BaseContainer, IExposeData
    {
        [ViewVariables]
        public IEntity? ContainedEntity
        {
            get => _containedEntity;
            private set
            {
                _containedEntity = value;
            }
        }
        
        private IEntity? _containedEntity;

        private const string ClassName = "ContainerSlot";

        /// <inheritdoc />
        public override string ContainerType => ClassName;

        /// <inheritdoc />
        public override IReadOnlyList<IEntity> ContainedEntities
        {
            get
            {
                if (ContainedEntity == null)
                {
                    return Array.Empty<IEntity>();
                }

                return new List<IEntity>{ContainedEntity};
            }
        }

        public ContainerSlot(string id, IContainerManager manager) : base(id, manager)
        {
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

        /// <inheritdoc />
        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataReadWriteFunction("showEnts", false, value => ShowContents = value, () => ShowContents);
            serializer.DataReadWriteFunction("occludes", false, value => OccludesLight = value, () => OccludesLight);
            serializer.DataField(ref _containedEntity, "ent", default);
        }
    }
}
