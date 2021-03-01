using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
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
    public sealed class Container : BaseContainer
    {
        private const string ClassName = "Container";

        /// <summary>
        /// The generic container class uses a list of entities
        /// </summary>
        private List<IEntity> _containerList = new();

        /// <inheritdoc />
        public override IReadOnlyList<IEntity> ContainedEntities => _containerList;

        /// <inheritdoc />
        public override string ContainerType => ClassName;
        
        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

#if SERV3
            // ONLY PAUL CAN MAKE ME WHOLE
            serializer.DataField(ref _containerList, "ents", new List<IEntity>());
#else
            if (serializer.Writing)
            {
                serializer.DataWriteFunction("ents", new List<EntityUid>(),
                    () => _containerList.Select(e => e.Uid).ToList());
            }
            else
            {
                var entMan = IoCManager.Resolve<IEntityManager>();

                serializer.DataReadFunction("ents", new List<EntityUid>(),
                    value => _containerList = value.Select((uid => entMan.GetEntity(uid))).ToList());
            }
#endif
        }

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
    }
}
