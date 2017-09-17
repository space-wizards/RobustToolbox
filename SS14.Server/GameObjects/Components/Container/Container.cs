using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;

namespace SS14.Server.GameObjects.Components.Container
{
    /// <summary>
    /// Default implementation for containers,
    /// cannot be inherited. If additional logic is needed,
    /// this logic should go on the systems that are holding this container.
    /// For example, inventory containers should be modified only through an inventory component.
    /// </summary>
    public sealed class Container : IContainer
    {
        /// <inheritdoc />
        public string ID { get; }

        /// <inheritdoc />
        public IContainerManager Manager { get; private set; }

        /// <inheritdoc />
        public IEntity Owner => Manager.Owner;

        /// <inheritdoc />
        public bool Deleted { get; private set; } = false;

        private List<IEntity> ContainerList;

        /// <summary>
        /// Shortcut method to make creation of containers easier.
        /// Creates a new container on the entity and gives it back to you.
        /// </summary>
        /// <param name="id">The ID of the new container.</param>
        /// <param name="entity">The entity to create the container for.</param>
        /// <returns>The new container.</returns>
        /// <exception cref="ArgumentException">Thrown if there already is a container with the specified ID.</exception>
        /// <seealso cref="IContainerManager.MakeContainer{T}(string)" />
        public static IContainer Create(string id, IEntity entity)
        {
            if (!entity.TryGetComponent<IContainerManager>(out var containermanager))
            {
                var factory = IoCManager.Resolve<IComponentFactory>();
                containermanager = factory.GetComponent<ContainerManagerComponent>();
                entity.AddComponent(containermanager);
            }

            return containermanager.MakeContainer<Container>(id);
        }

        /// <summary>
        /// DO NOT CALL THIS METHOD DIRECTLY!
        /// You want <see cref="IContainerManager.MakeContainer{T}(string)" /> or <see cref="Create" /> instead.
        /// </summary>
        internal Container(string id, IContainerManager manager)
        {
            ID = id;
            Manager = manager;
        }

        /// <inheritdoc />
        public bool CanInsert(IEntity toinsert)
        {
            // Crucial, prevent circular insertion.
            if (toinsert.GetComponent<ITransformComponent>().ContainsEntity(Owner.GetComponent<ITransformComponent>()))
            {
                throw new InvalidOperationException("Attempt to insert entity into one of its children.");
            }
            return true;
        }

        /// <inheritdoc />
        public bool Insert(IEntity toinsert)
        {
            if (CanInsert(toinsert) && toinsert.GetComponent<ITransformComponent>().Parent.Owner.GetComponent<IContainerManager>().Remove(toinsert)) //Verify we can insert and that the object got properly removed from its current location
            {
                ContainerList.Add(toinsert);
                toinsert.GetComponent<IServerTransformComponent>().AttachParent(Owner.GetComponent<ITransformComponent>());
                //OnInsert(); If necessary a component may add eventhandlers for this and delegate some functions to it
                return true;
            }
            return false;
        }

        /// <inheritdoc />
        public bool CanRemove(IEntity toremove)
        {
            return Contains(toremove);
        }

        /// <inheritdoc />
        public bool Remove(IEntity toremove)
        {
            if (!CanRemove(toremove))
            {
                return false;
            }
            ContainerList.Remove(toremove);
            toremove.GetComponent<IServerTransformComponent>().DetachParent();
            //OnRemoval(toremove); If necessary a component may add eventhandlers for this and delegate some functions to it
            return true;
        }

        /// <inheritdoc />
        public bool Contains(IEntity contained)
        {
            return ContainerList.Contains(contained);
        }

        /// <inheritdoc />
        public void Shutdown()
        {
            Deleted = true;
            Manager = null;
            foreach (var entity in ContainerList)
            {
                var transform = entity.GetComponent<IServerTransformComponent>();
                transform.DetachParent();
            }
        }
    }
}
