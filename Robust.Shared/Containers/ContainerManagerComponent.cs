using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Containers
{
    /// <summary>
    /// Holds data about a set of entity containers on this entity.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IContainerManager))]
    public class ContainerManagerComponent : Component, IContainerManager
    {
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;

        [ViewVariables] private Dictionary<string, IContainer> _containers = new();
        private Dictionary<string, List<EntityUid>>? _entitiesWaitingResolve;

        /// <inheritdoc />
        public sealed override string Name => "ContainerContainer";

        /// <inheritdoc />
        public sealed override uint? NetID => NetIDs.CONTAINER_MANAGER;

        /// <inheritdoc />
        public override void OnRemove()
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
        public override void Initialize()
        {
            base.Initialize();

            if (_entitiesWaitingResolve == null) return;

            foreach (var (key, entities) in _entitiesWaitingResolve)
            {
                var container = GetContainer(key);
                foreach (var uid in entities)
                {
                    container.Insert(Owner.EntityManager.GetEntity(uid));
                }
            }

            _entitiesWaitingResolve = null;
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
                if (!cast.Containers.ContainsKey(id))
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
            foreach (var (id, data) in cast.Containers)
            {
                if (!_containers.TryGetValue(id, out var container))
                {
                    container = new ClientContainer(id, this);
                    _containers.Add(id, container);
                }

                // sync show flag
                container.ShowContents = data.ShowContents;
                container.OccludesLight = data.OccludesLight;

                // Remove gone entities.
                List<IEntity>? toRemove = null;
                foreach (var entity in container.ContainedEntities)
                {
                    if (!data.ContainedEntities.Contains(entity.Uid))
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
                foreach (var uid in data.ContainedEntities)
                {
                    var entity = Owner.EntityManager.GetEntity(uid);

                    if (!container.ContainedEntities.Contains(entity)) container.Insert(entity);
                }
            }
        }

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            if (serializer.Reading)
            {
                if (serializer.TryReadDataField<Dictionary<string, ContainerPrototypeData>>("containers", out var data))
                {
                    _entitiesWaitingResolve = new Dictionary<string, List<EntityUid>>();
                    foreach (var (key, datum) in data)
                    {
                        if (datum.Type == null) throw new InvalidOperationException("Container does not have type set.");

                        var type = _reflectionManager.LooseGetType(datum.Type);
                        MakeContainer(key, type);

                        if (datum.Entities.Count == 0) continue;

                        var list = new List<EntityUid>(datum.Entities.Where(u => u.IsValid()));
                        _entitiesWaitingResolve.Add(key, list);
                    }
                }
            }
            else
            {
                serializer.DataField(ref _containers, "containers", new Dictionary<string, IContainer>());

                // var dict = new Dictionary<string, ContainerPrototypeData>();
                // foreach (var (key, container) in _containers)
                // {
                //     var list = new List<EntityUid>(container.ContainedEntities.Select(e => e.Uid));
                //     var data = new ContainerPrototypeData(list, container.GetType().FullName!);
                //     dict.Add(key, data);
                // }
                //
                // // ReSharper disable once RedundantTypeArgumentsOfMethod
                // serializer.DataWriteFunction<Dictionary<string, ContainerPrototypeData>?>("containers", null,
                //     () => dict);
            }
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new ContainerManagerComponentState(
                _containers.Values.ToDictionary(
                    c => c.ID,
                    container => (ContainerManagerComponentState.ContainerData) new()
                    {
                        ContainedEntities = container.ContainedEntities.Select(e => e.Uid).ToArray(),
                        ShowContents = container.ShowContents,
                        OccludesLight = container.OccludesLight
                    }));
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

            var container = (IContainer) Activator.CreateInstance(type, id, this)!;
            _containers[id] = container;
            Dirty();
            return container;
        }

        public AllContainersEnumerable GetAllContainers()
        {
            return new(this);
        }

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

        private struct ContainerPrototypeData : IExposeData
        {
            public List<EntityUid> Entities;
            public string? Type;

            public ContainerPrototypeData(List<EntityUid> entities, string type)
            {
                Entities = entities;
                Type = type;
            }

            void IExposeData.ExposeData(ObjectSerializer serializer)
            {
                serializer.DataField(ref Entities, "entities", new List<EntityUid>());
                serializer.DataField(ref Type, "type", null);
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
