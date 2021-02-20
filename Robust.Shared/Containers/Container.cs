using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Robust.Shared.Containers
{
    /// <summary>
    /// Default implementation for containers,
    /// cannot be inherited. If additional logic is needed,
    /// this logic should go on the systems that are holding this container.
    /// For example, inventory containers should be modified only through an inventory component.
    /// </summary>
    [UsedImplicitly]
    [SerializedType(ClassName)]
    public sealed class Container : BaseContainer, IExposeData
    {
        private const string ClassName = "Container";

        /// <inheritdoc />
        public override string ContainerType => ClassName;

        /// <summary>
        /// The generic container class uses a list of entities
        /// </summary>
        private List<IEntity> _containerList = new();

        /// <inheritdoc />
        public Container(string id, IContainerManager manager) : base(id, manager) { }

        /// <inheritdoc />
        public override IReadOnlyList<IEntity> ContainedEntities => _containerList;

        /// <inheritdoc />
        protected override void InternalInsert(IEntity toinsert)
        {
            _containerList.Add(toinsert);
            base.InternalInsert(toinsert);
        }

        /// <inheritdoc />
        protected override void InternalRemove(IEntity toremove)
        {
            _containerList.Remove(toremove);
            base.InternalRemove(toremove);
        }

        /// <inheritdoc />
        public override bool Contains(IEntity contained)
        {
            return _containerList.Contains(contained);
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            base.Shutdown();

            foreach (var entity in _containerList)
            {
                entity.Delete();
            }
        }

        /// <inheritdoc />
        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataReadWriteFunction("showEnts", false, value => ShowContents = value, () => ShowContents);
            serializer.DataReadWriteFunction("occludes", false, value => OccludesLight = value, () => OccludesLight);
            serializer.DataField(ref _containerList, "ents", new List<IEntity>());
        }
    }
}
