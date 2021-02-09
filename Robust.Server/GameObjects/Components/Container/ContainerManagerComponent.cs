using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.GameObjects.Components.Containers;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Server.GameObjects.Components.Container
{
    public sealed class ContainerManagerComponent : SharedContainerManagerComponent
    {
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;

        private readonly Dictionary<string, IContainer> EntityContainers = new();
        private Dictionary<string, List<EntityUid>>? _entitiesWaitingResolve;

        [ViewVariables] private IEnumerable<IContainer> _allContainers => EntityContainers.Values;

        /// <summary>
        /// Shortcut method to make creation of containers easier.
        /// Creates a new container on the entity and gives it back to you.
        /// </summary>
        /// <param name="id">The ID of the new container.</param>
        /// <param name="entity">The entity to create the container for.</param>
        /// <returns>The new container.</returns>
        /// <exception cref="ArgumentException">Thrown if there already is a container with the specified ID.</exception>
        /// <seealso cref="IContainerManager.MakeContainer{T}(string)" />
        public static T Create<T>(string id, IEntity entity) where T : IContainer
        {
            if (!entity.TryGetComponent<IContainerManager>(out var containermanager))
            {
                containermanager = entity.AddComponent<ContainerManagerComponent>();
            }

            return containermanager.MakeContainer<T>(id);
        }

        public static T Ensure<T>(string id, IEntity entity) where T : IContainer
        {
            return Ensure<T>(id, entity, out _);
        }

        public static T Ensure<T>(string id, IEntity entity, out bool alreadyExisted) where T : IContainer
        {
            var containerManager = entity.EnsureComponent<ContainerManagerComponent>();

            if (!containerManager.TryGetContainer(id, out var existing))
            {
                alreadyExisted = false;
                return containerManager.MakeContainer<T>(id);
            }

            if (!(existing is T container))
            {
                throw new InvalidOperationException(
                    $"The container exists but is of a different type: {existing.GetType()}");
            }

            alreadyExisted = true;
            return container;
        }

        public override T MakeContainer<T>(string id)
        {
            return (T) MakeContainer(id, typeof(T));
        }

        private IContainer MakeContainer(string id, Type type)
        {
            if (HasContainer(id))
            {
                throw new ArgumentException($"Container with specified ID already exists: '{id}'");
            }

            var container = (IContainer) Activator.CreateInstance(type, id, this)!;
            EntityContainers[id] = container;
            Dirty();
            return container;
        }

        public new AllContainersEnumerable GetAllContainers()
        {
            return new(this);
        }



        protected override IEnumerable<IContainer> GetAllContainersImpl()
        {
            return GetAllContainers();
        }

        /// <inheritdoc />
        public override IContainer GetContainer(string id)
        {
            return EntityContainers[id];
        }

        /// <inheritdoc />
        public override bool HasContainer(string id)
        {
            return EntityContainers.ContainsKey(id);
        }

        /// <inheritdoc />
        public override bool TryGetContainer(string id, [NotNullWhen(true)] out IContainer? container)
        {
            if (!HasContainer(id))
            {
                container = null;
                return false;
            }

            container = GetContainer(id);
            return true;
        }

        public override bool TryGetContainer(IEntity entity, [NotNullWhen(true)] out IContainer? container)
        {
            foreach (var contain in EntityContainers.Values)
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

        public override bool ContainsEntity(IEntity entity)
        {
            foreach (var container in EntityContainers.Values)
            {
                if (!container.Deleted && container.Contains(entity))
                {
                    return true;
                }
            }

            return false;
        }

        public override void ForceRemove(IEntity entity)
        {
            foreach (var container in EntityContainers.Values)
            {
                if (container.Contains(entity))
                {
                    container.ForceRemove(entity);
                }
            }
        }

        public override void InternalContainerShutdown(IContainer container)
        {
            EntityContainers.Remove(container.ID);
        }

        /// <inheritdoc />
        public override bool Remove(IEntity entity)
        {
            foreach (var containers in EntityContainers.Values)
            {
                if (containers.Contains(entity))
                {
                    return containers.Remove(entity);
                }
            }

            return true; // If we don't contain the entity, it will always be removed
        }

        public override void OnRemove()
        {
            base.OnRemove();

            // IContianer.Shutdown modifies the EntityContainers collection
            foreach (var container in EntityContainers.Values.ToArray())
            {
                container.Shutdown();
            }

            EntityContainers.Clear();
        }

        //TODO Paul: fix dis
        /*public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            if (serializer.Reading)
            {
                if (serializer.TryReadDataField<Dictionary<string, ContainerPrototypeData>>("containers", out var data))
                {
                    _entitiesWaitingResolve = new Dictionary<string, List<EntityUid>>();
                    foreach (var (key, datum) in data)
                    {
                        if (datum.Type == null)
                        {
                            throw new InvalidOperationException("Container does not have type set.");
                        }

                        var type = _reflectionManager.LooseGetType(datum.Type);
                        MakeContainer(key, type);

                        if (datum.Entities.Count == 0)
                        {
                            continue;
                        }

                        var list = new List<EntityUid>(datum.Entities.Where(u => u.IsValid()));
                        _entitiesWaitingResolve.Add(key, list);
                    }
                }
            }
            else
            {
                var dict = new Dictionary<string, ContainerPrototypeData>();
                foreach (var (key, container) in EntityContainers)
                {
                    var list = new List<EntityUid>(container.ContainedEntities.Select(e => e.Uid));
                    var data = new ContainerPrototypeData(list, container.GetType().FullName!);
                    dict.Add(key, data);
                }

                // ReSharper disable once RedundantTypeArgumentsOfMethod
                serializer.DataWriteFunction<Dictionary<string, ContainerPrototypeData>?>("containers", null,
                    () => dict);
            }
        }*/

        public override void Initialize()
        {
            base.Initialize();

            if (_entitiesWaitingResolve == null)
            {
                return;
            }

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

        public override ComponentState GetComponentState()
        {
            return new ContainerManagerComponentState(
                _allContainers.ToDictionary(
                    c => c.ID,
                    DataFor));
        }

        private static ContainerManagerComponentState.ContainerData DataFor(IContainer container)
        {
            return new()
            {
                ContainedEntities = container.ContainedEntities.Select(e => e.Uid).ToArray(),
                ShowContents = container.ShowContents,
                OccludesLight = container.OccludesLight
            };
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

            public void ExposeData(ObjectSerializer serializer)
            {
                serializer.DataField(ref Entities, "entities", new List<EntityUid>());
                serializer.DataField(ref Type, "type", null);
            }
        }

        public struct AllContainersEnumerable : IEnumerable<IContainer>
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
                _enumerator = manager.EntityContainers.Values.GetEnumerator();
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

            [AllowNull] public IContainer Current { get; private set; }

            object? IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }
    }
}
