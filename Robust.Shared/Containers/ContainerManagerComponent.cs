using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Players;
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
    [NetworkedComponent()]
    public class ContainerManagerComponent : Component, IContainerManager
    {
        [Dependency] private readonly IRobustSerializer _serializer = default!;
        [Dependency] private readonly IDynamicTypeFactoryInternal _dynFactory = default!;

        [ViewVariables]
        [DataField("containers")]
        private Dictionary<string, IContainer> _containers = new();

        /// <inheritdoc />
        public sealed override string Name => "ContainerContainer";

        /// <inheritdoc />
        protected override void OnRemove()
        {
            base.OnRemove();

            // IContianer.Shutdown modifies the _containers collection
            foreach (var container in _containers.Values.ToArray())
            {
                container.Shutdown();
            }

            _containers.Clear();
        }

        /// <inheritdoc />
        protected override void Initialize()
        {
            base.Initialize();

            foreach (var container in _containers)
            {
                var baseContainer = (BaseContainer)container.Value;
                baseContainer.Manager = this;
                baseContainer.ID = container.Key;
            }
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (!(curState is ContainerManagerComponentState cast))
                return;

            // Delete now-gone containers.
            List<string>? toDelete = null;
            foreach (var (id, container) in _containers)
            {
                if (!cast.ContainerSet.Any(data => data.Id == id))
                {
                    container.Shutdown();
                    toDelete ??= new List<string>();
                    toDelete.Add(id);
                }
            }

            if (toDelete != null)
            {
                foreach (var dead in toDelete)
                {
                    _containers.Remove(dead);
                }
            }

            // Add new containers and update existing contents.

            foreach (var (containerType, id, showEnts, occludesLight, entityUids) in cast.ContainerSet)
            {
                if (!_containers.TryGetValue(id, out var container))
                {
                    container = ContainerFactory(containerType, id);
                    _containers.Add(id, container);
                }

                // sync show flag
                container.ShowContents = showEnts;
                container.OccludesLight = occludesLight;

                // Remove gone entities.
                List<IEntity>? toRemove = null;
                foreach (var entity in container.ContainedEntities)
                {
                    if (!entityUids.Contains(entity.Uid))
                    {
                        toRemove ??= new List<IEntity>();
                        toRemove.Add(entity);
                    }
                }

                if (toRemove != null)
                {
                    foreach (var goner in toRemove)
                    {
                        container.Remove(goner);
                    }
                }

                // Add new entities.
                foreach (var uid in entityUids)
                {
                    var entity = Owner.EntityManager.GetEntity(uid);

                    if (!container.ContainedEntities.Contains(entity)) container.Insert(entity);
                }
            }
        }

        private IContainer ContainerFactory(string containerType, string id)
        {
            var type = _serializer.FindSerializedType(typeof(IContainer), containerType);
            if (type is null) throw new ArgumentException($"Container of type {containerType} for id {id} cannot be found.");

            var newContainer = _dynFactory.CreateInstanceUnchecked<BaseContainer>(type);
            newContainer.ID = id;
            newContainer.Manager = this;
            return newContainer;
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState(ICommonSession player)
        {
            // naive implementation that just sends the full state of the component
            List<ContainerManagerComponentState.ContainerData> containerSet = new();

            foreach (var container in _containers.Values)
            {
                var uidArr = new EntityUid[container.ContainedEntities.Count];

                for (var index = 0; index < container.ContainedEntities.Count; index++)
                {
                    var iEntity = container.ContainedEntities[index];
                    uidArr[index] = iEntity.Uid;
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
            return _containers[id];
        }

        /// <inheritdoc />
        public bool HasContainer(string id)
        {
            return _containers.ContainsKey(id);
        }

        /// <inheritdoc />
        public bool TryGetContainer(string id, [NotNullWhen(true)] out IContainer? container)
        {
            var ret = _containers.TryGetValue(id, out var cont);
            container = cont!;
            return ret;
        }

        /// <inheritdoc />
        public bool TryGetContainer(IEntity entity, [NotNullWhen(true)] out IContainer? container)
        {
            foreach (var contain in _containers.Values)
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
        public bool ContainsEntity(IEntity entity)
        {
            foreach (var container in _containers.Values)
            {
                if (!container.Deleted && container.Contains(entity)) return true;
            }

            return false;
        }

        /// <inheritdoc />
        public void ForceRemove(IEntity entity)
        {
            foreach (var container in _containers.Values)
            {
                if (container.Contains(entity)) container.ForceRemove(entity);
            }
        }

        /// <inheritdoc />
        public void InternalContainerShutdown(IContainer container)
        {
            _containers.Remove(container.ID);
        }

        /// <inheritdoc />
        public bool Remove(IEntity entity)
        {
            foreach (var containers in _containers.Values)
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
            foreach (var container in _containers.Values)
            {
                foreach (var containerEntity in container.ContainedEntities)
                {
                    Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local,
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

            _containers[id] = container;
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
            private readonly ContainerManagerComponent _manager;

            public AllContainersEnumerable(ContainerManagerComponent manager)
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

            public AllContainersEnumerator(ContainerManagerComponent manager)
            {
                _enumerator = manager._containers.Values.GetEnumerator();
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
