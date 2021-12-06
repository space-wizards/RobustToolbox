using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Containers
{
    /// <summary>
    /// Holds data about a set of entity containers on this entity.
    /// </summary>
    [ComponentReference(typeof(IContainerManager))]
    [NetworkedComponent]
    public class ContainerManagerComponent : Component, IContainerManager, ISerializationHooks
    {
        [Dependency] private readonly IDynamicTypeFactoryInternal _dynFactory = default!;

        [ViewVariables]
        [DataField("containers")]
        public Dictionary<string, IContainer> Containers = new();

        /// <inheritdoc />
        public sealed override string Name => "ContainerContainer";

        void ISerializationHooks.AfterDeserialization()
        {
            foreach (var (_, container) in Containers)
            {
                var baseContainer = (BaseContainer) container;
                baseContainer.Manager = this;
            }
        }

        /// <inheritdoc />
        protected override void OnRemove()
        {
            base.OnRemove();

            // IContainer.Shutdown modifies the _containers collection
            foreach (var container in Containers.Values.ToArray())
            {
                container.Shutdown();
            }

            Containers.Clear();
        }

        /// <inheritdoc />
        protected override void Initialize()
        {
            base.Initialize();

            foreach (var container in Containers)
            {
                var baseContainer = (BaseContainer)container.Value;
                baseContainer.Manager = this;
                baseContainer.ID = container.Key;
            }
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            // naive implementation that just sends the full state of the component
            List<ContainerManagerComponentState.ContainerData> containerSet = new(Containers.Count);

            foreach (var container in Containers.Values)
            {
                var uidArr = new EntityUid[container.ContainedEntities.Count];

                for (var index = 0; index < container.ContainedEntities.Count; index++)
                {
                    uidArr[index] = container.ContainedEntities[index];
                }

                var sContainer = new ContainerManagerComponentState.ContainerData(container.ContainerType, container.ID, container.ShowContents, container.OccludesLight, uidArr);
                containerSet.Add(sContainer);
            }

            return new ContainerManagerComponentState(containerSet);
        }

        /// <inheritdoc />
        public T MakeContainer<T>(string id)
            where T : IContainer
        {
            return (T) MakeContainer(id, typeof(T));
        }

        /// <inheritdoc />
        public IContainer GetContainer(string id)
        {
            return Containers[id];
        }

        /// <inheritdoc />
        public bool HasContainer(string id)
        {
            return Containers.ContainsKey(id);
        }

        /// <inheritdoc />
        public bool TryGetContainer(string id, [NotNullWhen(true)] out IContainer? container)
        {
            var ret = Containers.TryGetValue(id, out var cont);
            container = cont!;
            return ret;
        }

        /// <inheritdoc />
        public bool TryGetContainer(EntityUid entity, [NotNullWhen(true)] out IContainer? container)
        {
            foreach (var contain in Containers.Values)
            {
                if (!contain.Deleted && contain.Contains(entity))
                {
                    container = contain;
                    return true;
                }
            }

            container = default;
            return false;
        }

        /// <inheritdoc />
        public bool ContainsEntity(EntityUid entity)
        {
            foreach (var container in Containers.Values)
            {
                if (!container.Deleted && container.Contains(entity)) return true;
            }

            return false;
        }

        /// <inheritdoc />
        public void ForceRemove(EntityUid entity)
        {
            foreach (var container in Containers.Values)
            {
                if (container.Contains(entity)) container.ForceRemove(entity);
            }
        }

        /// <inheritdoc />
        public void InternalContainerShutdown(IContainer container)
        {
            Containers.Remove(container.ID);
        }

        /// <inheritdoc />
        public bool Remove(EntityUid entity)
        {
            foreach (var containers in Containers.Values)
            {
                if (containers.Contains(entity)) return containers.Remove(entity);
            }

            return true; // If we don't contain the entity, it will always be removed
        }

        /// <inheritdoc />
        protected override void Shutdown()
        {
            base.Shutdown();

            // On shutdown we won't get to process remove events in the containers so this has to be manually done.
            var entMan = IoCManager.Resolve<IEntityManager>();
            foreach (var container in Containers.Values)
            {
                foreach (var containerEntity in container.ContainedEntities)
                {
                    entMan.EventBus.RaiseEvent(EventSource.Local,
                        new UpdateContainerOcclusionMessage(containerEntity));
                }
            }
        }

        private IContainer MakeContainer(string id, Type type)
        {
            if (HasContainer(id)) throw new ArgumentException($"Container with specified ID already exists: '{id}'");

            var container = _dynFactory.CreateInstanceUnchecked<BaseContainer>(type);
            container.ID = id;
            container.Manager = this;

            Containers[id] = container;
            Dirty();
            return container;
        }

        public AllContainersEnumerable GetAllContainers()
        {
            return new(this);
        }

        [Serializable, NetSerializable]
        internal class ContainerManagerComponentState : ComponentState
        {
            public List<ContainerData> ContainerSet;

            public ContainerManagerComponentState(List<ContainerData> containers)
            {
                ContainerSet = containers;
            }

            [Serializable, NetSerializable]
            public readonly struct ContainerData
            {
                public readonly string ContainerType;
                public readonly string Id;
                public readonly bool ShowContents;
                public readonly bool OccludesLight;
                public readonly EntityUid[] ContainedEntities;

                public ContainerData(string containerType, string id, bool showContents, bool occludesLight, EntityUid[] containedEntities)
                {
                    ContainerType = containerType;
                    Id = id;
                    ShowContents = showContents;
                    OccludesLight = occludesLight;
                    ContainedEntities = containedEntities;
                }

                public void Deconstruct(out string type, out string id, out bool showEnts, out bool occludesLight, out EntityUid[] ents)
                {
                    type = ContainerType;
                    id = Id;
                    showEnts = ShowContents;
                    occludesLight = OccludesLight;
                    ents = ContainedEntities;
                }
            }
        }

        [DataDefinition]
        private struct ContainerPrototypeData : IPopulateDefaultValues
        {
            [DataField("entities")]
            public List<EntityUid> Entities;

            [DataField("type")]
            public string? Type;

            public ContainerPrototypeData(List<EntityUid> entities, string type)
            {
                Entities = entities;
                Type = type;
            }

            public void PopulateDefaultValues()
            {
                Entities = new List<EntityUid>();
            }
        }

        public readonly struct AllContainersEnumerable : IEnumerable<IContainer>
        {
            private readonly ContainerManagerComponent? _manager;

            public AllContainersEnumerable(ContainerManagerComponent? manager)
            {
                _manager = manager;
            }

            public AllContainersEnumerator GetEnumerator()
            {
                return new(_manager);
            }

            IEnumerator<IContainer> IEnumerable<IContainer>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public struct AllContainersEnumerator : IEnumerator<IContainer>
        {
            private Dictionary<string, IContainer>.ValueCollection.Enumerator _enumerator;

            public AllContainersEnumerator(ContainerManagerComponent? manager)
            {
                _enumerator = manager?.Containers.Values.GetEnumerator() ?? new();
                Current = default;
            }

            public bool MoveNext()
            {
                while (_enumerator.MoveNext())
                {
                    if (!_enumerator.Current.Deleted)
                    {
                        Current = _enumerator.Current;
                        return true;
                    }
                }

                return false;
            }

            void IEnumerator.Reset()
            {
                ((IEnumerator<IContainer>) _enumerator).Reset();
            }

            [AllowNull]
            public IContainer Current { get; private set; }

            object IEnumerator.Current => Current;

            public void Dispose() { }
        }
    }
}
