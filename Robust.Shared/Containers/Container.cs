using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

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
    public sealed class Container : BaseContainer
    {
        private const string ClassName = "Container";

        /// <summary>
        /// The generic container class uses a list of entities
        /// </summary>
        [DataField("ents")]
        private readonly List<IEntity> _containerList = new();

        private readonly List<EntityUid> _expectedEntities = new();

        /// <inheritdoc />
        public override IReadOnlyList<IEntity> ContainedEntities => _containerList;

        public override List<EntityUid> ExpectedEntities => _expectedEntities;

        /// <inheritdoc />
        public override string ContainerType => ClassName;

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
                IoCManager.Resolve<IEntityManager>().DeleteEntity((EntityUid) entity);
            }
        }
    }
}
