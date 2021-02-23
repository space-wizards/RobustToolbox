using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects
{
    public abstract class SharedContainerManagerComponent : Component, IContainerManager
    {
        public sealed override string Name => "ContainerContainer";
        public sealed override uint? NetID => NetIDs.CONTAINER_MANAGER;

        public abstract T MakeContainer<T>(string id) where T : IContainer;
        public abstract bool Remove(IEntity entity);
        public abstract IContainer GetContainer(string id);
        public abstract bool HasContainer(string id);
        public abstract bool TryGetContainer(string id, [NotNullWhen(true)] out IContainer? container);

        /// <inheritdoc />
        public abstract bool TryGetContainer(IEntity entity, [NotNullWhen(true)] out IContainer? container);

        public abstract bool ContainsEntity(IEntity entity);
        public abstract void ForceRemove(IEntity entity);
        public abstract void InternalContainerShutdown(IContainer container);

        [Serializable, NetSerializable]
        protected class ContainerManagerComponentState : ComponentState
        {
            public Dictionary<string, ContainerData> Containers { get; }

            public ContainerManagerComponentState(Dictionary<string, ContainerData> containers) : base(NetIDs.CONTAINER_MANAGER)
            {
                Containers = containers;
            }

            [Serializable, NetSerializable]
            public struct ContainerData
            {
                public bool ShowContents;
                public bool OccludesLight;
                public EntityUid[] ContainedEntities;
            }
        }

        public IEnumerable<IContainer> GetAllContainers() => GetAllContainersImpl();

        // Separate impl method to facilitate method hiding in the subclasses.
        protected abstract IEnumerable<IContainer> GetAllContainersImpl();
    }
}
