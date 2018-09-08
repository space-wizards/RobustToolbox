using System;
using System.Collections.Generic;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.ViewVariables;

namespace SS14.Server.GameObjects.Components.Container
{
    /// <summary>
    /// Default implementation for containers,
    /// cannot be inherited. If additional logic is needed,
    /// this logic should go on the systems that are holding this container.
    /// For example, inventory containers should be modified only through an inventory component.
    /// </summary>
    public sealed class Container : BaseContainer
    {
        /// <summary>
        /// The generic container class uses a list of entities
        /// </summary>
        private readonly List<IEntity> ContainerList = new List<IEntity>();

        /// <inheritdoc />
        public Container(string id, IContainerManager manager) : base(id, manager)
        {
        }

        /// <inheritdoc />
        public override IReadOnlyCollection<IEntity> ContainedEntities => ContainerList.AsReadOnly();

        /// <inheritdoc />
        protected override void InternalInsert(IEntity toinsert)
        {
            ContainerList.Add(toinsert);
        }

        /// <inheritdoc />
        protected override void InternalRemove(IEntity toremove)
        {
            ContainerList.Remove(toremove);
        }

        /// <inheritdoc />
        public override bool Contains(IEntity contained)
        {
            return ContainerList.Contains(contained);
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            base.Shutdown();

            foreach (var entity in ContainerList)
            {
                var transform = entity.GetComponent<ITransformComponent>();
                transform.DetachParent();
            }
        }
    }

    public abstract class BaseContainer : IContainer
    {
        /// <inheritdoc />
        public IContainerManager Manager { get; protected set; }

        /// <inheritdoc />
        [ViewVariables]
        public string ID { get; }

        /// <inheritdoc />
        [ViewVariables]
        public IEntity Owner => Manager.Owner;

        /// <inheritdoc />
        [ViewVariables]
        public bool Deleted { get; protected set; } = false;

        /// <inheritdoc />
        public abstract IReadOnlyCollection<IEntity> ContainedEntities { get; }

        /// <summary>
        /// DO NOT CALL THIS METHOD DIRECTLY!
        /// You want <see cref="IContainerManager.MakeContainer{T}(string)" /> or <see cref="Create" /> instead.
        /// </summary>
        protected BaseContainer(string id, IContainerManager manager)
        {
            ID = id;
            Manager = manager;
        }

        /// <inheritdoc />
        public bool Insert(IEntity toinsert)
        {
            if (CanInsert(toinsert)) //Verify we can insert and that the object got properly removed from its current location
            {
                var transform = toinsert.GetComponent<ITransformComponent>();
                if (!transform.IsMapTransform && !transform.Parent.Owner.GetComponent<IContainerManager>().Remove(toinsert))
                {
                    // Can't detach the entity from its parent, can't insert.
                    return false;
                }
                InternalInsert(toinsert);
                transform.AttachParent(Owner.GetComponent<ITransformComponent>());
                return true;
            }
            return false;
        }

        /// <summary>
        /// Implement to store the reference in whatever form you want
        /// </summary>
        /// <param name="toinsert"></param>
        protected abstract void InternalInsert(IEntity toinsert);

        /// <inheritdoc />
        public virtual bool CanInsert(IEntity toinsert)
        {
            // Crucial, prevent circular insertion.
            if (toinsert.GetComponent<ITransformComponent>().ContainsEntity(Owner.GetComponent<ITransformComponent>()))
            {
                throw new InvalidOperationException("Attempt to insert entity into one of its children.");
            }
            return true;
        }

        /// <inheritdoc />
        public bool Remove(IEntity toremove)
        {
            if (!CanRemove(toremove))
            {
                return false;
            }
            InternalRemove(toremove);
            toremove.GetComponent<ITransformComponent>().DetachParent();
            return true;
        }

        /// <summary>
        /// Implement to remove the reference you used to store the entity
        /// </summary>
        /// <param name="toinsert"></param>
        protected abstract void InternalRemove(IEntity toremove);

        /// <inheritdoc />
        public virtual bool CanRemove(IEntity toremove)
        {
            return Contains(toremove);
        }

        /// <inheritdoc />
        public abstract bool Contains(IEntity contained);

        /// <inheritdoc />
        public virtual void Shutdown()
        {
            Deleted = true;
            Manager = null;
        }
    }
}
