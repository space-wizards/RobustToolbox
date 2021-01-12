using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Server.GameObjects.EntitySystemMessages;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Server.GameObjects.Components.Container
{
    /// <summary>
    /// Default implementation for containers,
    /// cannot be inherited. If additional logic is needed,
    /// this logic should go on the systems that are holding this container.
    /// For example, inventory containers should be modified only through an inventory component.
    /// </summary>
    [UsedImplicitly]
    public sealed class Container : BaseContainer
    {
        /// <summary>
        /// The generic container class uses a list of entities
        /// </summary>
        private readonly List<IEntity> _containerList = new();

        /// <inheritdoc />
        public Container(string id, IContainerManager manager) : base(id, manager) { }

        /// <inheritdoc />
        public override IReadOnlyList<IEntity> ContainedEntities => _containerList;

        /// <inheritdoc />
        protected override void InternalInsert(IEntity toinsert)
        {
            _containerList.Add(toinsert);
            base.InternalInsert(toinsert);
        }

        /// <inheritdoc />
        protected override void InternalRemove(IEntity toremove)
        {
            _containerList.Remove(toremove);
            base.InternalRemove(toremove);
        }

        /// <inheritdoc />
        public override bool Contains(IEntity contained)
        {
            return _containerList.Contains(contained);
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            base.Shutdown();

            foreach (var entity in _containerList)
            {
                entity.Delete();
            }
        }
    }

    /// <summary>
    /// Base container class that all container inherit from.
    /// </summary>
    public abstract class BaseContainer : IContainer
    {
        /// <inheritdoc />
        public IContainerManager Manager { get; private set; }

        /// <inheritdoc />
        [ViewVariables]
        public string ID { get; }

        /// <inheritdoc />
        [ViewVariables]
        public IEntity Owner => Manager.Owner;

        /// <inheritdoc />
        [ViewVariables]
        public bool Deleted { get; private set; }

        /// <inheritdoc />
        [ViewVariables]
        public abstract IReadOnlyList<IEntity> ContainedEntities { get; }

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public bool ShowContents { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool OccludesLight { get; set; }

        /// <summary>
        /// DO NOT CALL THIS METHOD DIRECTLY!
        /// You want <see cref="IContainerManager.MakeContainer{T}(string)" /> instead.
        /// </summary>
        protected BaseContainer(string id, IContainerManager manager)
        {
            DebugTools.Assert(!string.IsNullOrEmpty(id));
            DebugTools.AssertNotNull(manager);

            ID = id;
            Manager = manager;
        }

        /// <inheritdoc />
        public bool Insert(IEntity toinsert)
        {
            DebugTools.Assert(!Deleted);

            //Verify we can insert and that the object got properly removed from its current location
            if (!CanInsert(toinsert))
                return false;

            var transform = toinsert.Transform;

            if (transform.Parent == null) // Only true if Parent is the map entity
                return false;

            if(transform.Parent.Owner.TryGetContainerMan(out var containerManager) && !containerManager.Remove(toinsert))
            {
                // Can't remove from existing container, can't insert.
                return false;
            }
            InternalInsert(toinsert);
            transform.AttachParent(Owner.Transform);

            // spatially move the object to the location of the container. If you don't want this functionality, the
            // calling code can save the local position before calling this function, and apply it afterwords.
            transform.LocalPosition = Vector2.Zero;

            return true;
        }

        /// <summary>
        /// Implement to store the reference in whatever form you want
        /// </summary>
        /// <param name="toinsert"></param>
        protected virtual void InternalInsert(IEntity toinsert)
        {
            DebugTools.Assert(!Deleted);

            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new EntInsertedIntoContainerMessage(toinsert, this));
            Manager.Owner.SendMessage(Manager, new ContainerContentsModifiedMessage(this, toinsert, false));
            Manager.Dirty();
        }

        /// <inheritdoc />
        public virtual bool CanInsert(IEntity toinsert)
        {
            DebugTools.Assert(!Deleted);

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
            DebugTools.Assert(!Deleted);

            if (toremove == null)
                return true;

            if (!CanRemove(toremove))
            {
                return false;
            }
            InternalRemove(toremove);

            if (!toremove.IsValid())
                return true;

            toremove.Transform.AttachParentToContainerOrGrid();
            return true;
        }

        /// <inheritdoc />
        public void ForceRemove(IEntity toRemove)
        {
            DebugTools.Assert(!Deleted);

            InternalRemove(toRemove);
        }

        /// <summary>
        /// Implement to remove the reference you used to store the entity
        /// </summary>
        /// <param name="toremove"></param>
        protected virtual void InternalRemove(IEntity toremove)
        {
            DebugTools.Assert(!Deleted);
            DebugTools.AssertNotNull(Manager);
            DebugTools.AssertNotNull(toremove);
            DebugTools.Assert(toremove.IsValid());

            Owner?.EntityManager.EventBus.RaiseEvent(EventSource.Local, new EntRemovedFromContainerMessage(toremove, this));

            Manager.Owner.SendMessage(Manager, new ContainerContentsModifiedMessage(this, toremove, true));
            Manager.Dirty();
        }

        /// <inheritdoc />
        public virtual bool CanRemove(IEntity toremove)
        {
            DebugTools.Assert(!Deleted);
            return Contains(toremove);
        }

        /// <inheritdoc />
        public abstract bool Contains(IEntity contained);

        /// <inheritdoc />
        public virtual void Shutdown()
        {
            Manager.InternalContainerShutdown(this);
            Deleted = true;
        }
    }

    /// <summary>
    /// The contents of this container have been changed.
    /// </summary>
    public class ContainerContentsModifiedMessage : ComponentMessage
    {
        /// <summary>
        /// Container whose contents were modified.
        /// </summary>
        public IContainer Container { get; }

        /// <summary>
        /// Entity that was added or removed from the container.
        /// </summary>
        public IEntity Entity { get; }

        /// <summary>
        /// If true, the entity was removed. If false, it was added to the container.
        /// </summary>
        public bool Removed { get; }

        /// <summary>
        /// Constructs a new instance of <see cref="ContainerContentsModifiedMessage"/>.
        /// </summary>
        /// <param name="container">Container whose contents were modified.</param>
        /// <param name="entity">Entity that was added or removed in the container.</param>
        /// <param name="removed">If true, the entity was removed. If false, it was added to the container.</param>
        public ContainerContentsModifiedMessage(IContainer container, IEntity entity, bool removed)
        {
            Container = container;
            Entity = entity;
            Removed = removed;
        }
    }
}
