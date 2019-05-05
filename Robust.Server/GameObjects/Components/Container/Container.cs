using System.Collections.Generic;
using Robust.Server.GameObjects.EntitySystemMessages;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.ViewVariables;

namespace Robust.Server.GameObjects.Components.Container
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
            base.InternalInsert(toinsert);
        }

        /// <inheritdoc />
        protected override void InternalRemove(IEntity toremove)
        {
            ContainerList.Remove(toremove);
            base.InternalRemove(toremove);
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
                entity.Delete();
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
                var transform = toinsert.Transform;
                // The transform.Parent.Owner != Owner is there because map deserialization of containers still uses Insert()
                // In which case the child is already parented. To us. Don't reject him hand him to the orphanage.
                // Perhaps making it not use Insert() is a good idea but eh.
                if (!transform.IsMapTransform && transform.Parent.Owner != Owner && !transform.Parent.Owner.GetComponent<IContainerManager>().Remove(toinsert))
                {
                    // Can't detach the entity from its parent, can't insert.
                    return false;
                }
                InternalInsert(toinsert);
                transform.AttachParent(Owner.Transform);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Implement to store the reference in whatever form you want
        /// </summary>
        /// <param name="toinsert"></param>
        protected virtual void InternalInsert(IEntity toinsert)
        {
            Owner.EntityManager.RaiseEvent(Owner, new EntInsertedIntoContainerMessage(toinsert, this));
        }

        /// <inheritdoc />
        public virtual bool CanInsert(IEntity toinsert)
        {
            // cannot insert into itself.
            if (Owner == toinsert)
                return false;

            // Crucial, prevent circular insertion.
            return !toinsert.Transform.ContainsEntity(Owner.Transform);

            //Improvement: Traverse the entire tree to make sure we are not creating a loop.
        }

        /// <inheritdoc />
        public bool Remove(IEntity toremove)
        {
            if (toremove == null)
                return true;

            if (!CanRemove(toremove))
            {
                return false;
            }
            InternalRemove(toremove);

            if (!toremove.IsValid())
                return true;

            toremove.Transform.DetachParent();
            return true;
        }

        public void ForceRemove(IEntity toRemove)
        {
            InternalRemove(toRemove);
        }

        /// <summary>
        /// Implement to remove the reference you used to store the entity
        /// </summary>
        /// <param name="toremove"></param>
        protected virtual void InternalRemove(IEntity toremove)
        {
            Owner.EntityManager.RaiseEvent(Owner, new EntRemovedFromContainerMessage(toremove, this));
        }

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
