using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Robust.Server.GameObjects.Components.Container
{
    public class ContainerManagerComponent : Component, IContainerManager
    {
        public override string Name => "ContainerContainer";

#pragma warning disable 649
        [Dependency] private readonly IReflectionManager _reflectionManager;
#pragma warning restore 649

        private readonly Dictionary<string, IContainer> EntityContainers = new Dictionary<string, IContainer>();
        private Dictionary<string, List<EntityUid>> _entitiesWaitingResolve;

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
            if (!entity.TryGetComponent<IContainerManager>(out var containerManager))
            {
                containerManager = entity.AddComponent<ContainerManagerComponent>();
            }

            if (!containerManager.TryGetContainer(id, out var existing))
            {
                alreadyExisted = false;
                return containerManager.MakeContainer<T>(id);
            }

            if (!(existing is T container))
            {
                throw new InvalidOperationException($"The container exists but is of a different type: {existing.GetType()}");
            }

            alreadyExisted = true;
            return container;
        }

        /// <inheritdoc />
        public T MakeContainer<T>(string id) where T: IContainer
        {
            return (T) MakeContainer(id, typeof(T));
        }

        private IContainer MakeContainer(string id, Type type)
        {
            if (HasContainer(id))
            {
                throw new ArgumentException($"Container with specified ID already exists: '{id}'");
            }
            var container = (IContainer)Activator.CreateInstance(type, id, this);
            EntityContainers[id] = container;
            return container;
        }

        /// <inheritdoc />
        public IContainer GetContainer(string id)
        {
            return EntityContainers[id];
        }

        /// <inheritdoc />
        public bool HasContainer(string id)
        {
            return EntityContainers.ContainsKey(id);
        }

        /// <inheritdoc />
        public bool TryGetContainer(string id, out IContainer container)
        {
            if (!HasContainer(id))
            {
                container = null;
                return false;
            }
            container = GetContainer(id);
            return true;
        }

        public void ForceRemove(IEntity entity)
        {
            foreach (var containers in EntityContainers.Values)
            {
                if (containers.Contains(entity))
                {
                    containers.ForceRemove(entity);
                }
            }
        }

        /// <inheritdoc />
        public bool Remove(IEntity entity)
        {
            foreach (var containers in EntityContainers.Values)
            {
                if (containers.Contains(entity))
                {
                    return containers.Remove(entity);
                }
            }
            return false;
        }

        public override void OnRemove()
        {
            base.OnRemove();

            foreach(var container in EntityContainers.Values)
            {
                container.Shutdown();
            }
            EntityContainers.Clear();
        }

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
                    var data = new ContainerPrototypeData(list, container.GetType().FullName);
                    dict.Add(key, data);
                }

                serializer.DataWriteFunction("containers", null, () => dict);
            }
        }

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

        private struct ContainerPrototypeData : IExposeData
        {
            public List<EntityUid> Entities;
            public string Type;

            public ContainerPrototypeData(List<EntityUid> entities, string type)
            {
                Entities = entities;
                Type = type;
            }

            public void ExposeData(ObjectSerializer serializer)
            {
                serializer.DataField(ref Entities, "entities", null);
                serializer.DataField(ref Type, "type", null);
            }
        }
    }
}
