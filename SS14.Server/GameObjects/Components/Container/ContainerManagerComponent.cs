using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using System;
using System.Collections.Generic;

namespace SS14.Server.GameObjects.Components.Container
{
    public class ContainerManagerComponent : Component, IContainerManager
    {
        public override string Name => "ContainerContainer";

        private readonly Dictionary<string, IContainer> EntityContainers = new Dictionary<string, IContainer>();

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

        /// <inheritdoc />
        public T MakeContainer<T>(string id) where T: IContainer
        {
            if (HasContainer(id))
            {
                throw new ArgumentException($"Container with specified ID already exists: '{id}'");
            }
            T container = (T)Activator.CreateInstance(typeof(T), id, this);
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

        public override void Shutdown()
        {
            base.Shutdown();
            foreach(var container in EntityContainers.Values)
            {
                container.Shutdown();
            }
            EntityContainers.Clear();
        }
    }
}
