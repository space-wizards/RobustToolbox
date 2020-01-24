using System;
using System.Collections.Generic;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects.Components.Containers
{
    public abstract class SharedContainerManagerComponent : Component, IContainerManager
    {
        public sealed override string Name => "ContainerContainer";
        public sealed override uint? NetID => NetIDs.CONTAINER_MANAGER;

        public abstract T MakeContainer<T>(string id) where T : IContainer;
        public abstract bool Remove(IEntity entity);
        public abstract IContainer GetContainer(string id);
        public abstract bool HasContainer(string id);
        public abstract bool TryGetContainer(string id, out IContainer container);

        /// <inheritdoc />
        public abstract bool TryGetContainer(IEntity entity, out IContainer container);

        public abstract bool ContainsEntity(IEntity entity);
        public abstract void ForceRemove(IEntity entity);

        [Serializable, NetSerializable]
        protected class ContainerManagerComponentState : ComponentState
        {
            public Dictionary<string,(bool, List<EntityUid>)> Containers { get; }

            public ContainerManagerComponentState(Dictionary<string, (bool, List<EntityUid>)> containers) : base(NetIDs.CONTAINER_MANAGER)
            {
                Containers = containers;
            }
        }
    }
}
