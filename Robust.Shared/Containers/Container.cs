using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
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
    [SerializedType(ClassName)]
    public sealed class Container : BaseContainer
    {
        private const string ClassName = "Container";

        /// <summary>
        /// The generic container class uses a list of entities
        /// </summary>
        [DataField("ents")]
        private readonly List<EntityUid> _containerList = new();

        private readonly List<EntityUid> _expectedEntities = new();

        /// <inheritdoc />
        public override IReadOnlyList<EntityUid> ContainedEntities => _containerList;

        public override List<EntityUid> ExpectedEntities => _expectedEntities;

        /// <inheritdoc />
        public override string ContainerType => ClassName;

        /// <inheritdoc />
        protected override void InternalInsert(EntityUid toInsert, IEntityManager entMan)
        {
            DebugTools.Assert(!_containerList.Contains(toInsert));
            _containerList.Add(toInsert);
        }

        /// <inheritdoc />
        protected override void InternalRemove(EntityUid toRemove, IEntityManager entMan)
        {
            _containerList.Remove(toRemove);
        }

        /// <inheritdoc />
        public override bool Contains(EntityUid contained)
        {
            if (!_containerList.Contains(contained))
                return false;

            var flags = IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(contained).Flags;
            DebugTools.Assert((flags & MetaDataFlags.InContainer) != 0);

            return true;
        }

        /// <inheritdoc />
        protected override void InternalShutdown(IEntityManager entMan, bool isClient)
        {
            foreach (var entity in _containerList.ToArray())
            {
                if (!isClient)
                    entMan.DeleteEntity(entity);
                else if (entMan.EntityExists(entity))
                    Remove(entity, entMan, reparent: false, force: true);
            }
        }
    }
}
