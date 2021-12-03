using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Containers
{
    /// <summary>
    /// Base container class that all container inherit from.
    /// </summary>
    public abstract class BaseContainer : IContainer
    {
        /// <inheritdoc />
        [ViewVariables]
        public abstract IReadOnlyList<IEntity> ContainedEntities { get; }

        [ViewVariables]
        public abstract List<EntityUid> ExpectedEntities { get; }

        /// <inheritdoc />
        public abstract string ContainerType { get; }

        /// <inheritdoc />
        [ViewVariables]
        public bool Deleted { get; private set; }

        /// <inheritdoc />
        [ViewVariables]
        public string ID { get; internal set; } = default!; // Make sure you set me in init

        /// <inheritdoc />
        public IContainerManager Manager { get; internal set; } = default!; // Make sure you set me in init

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("occludes")]
        public bool OccludesLight { get; set; } = true;

        /// <inheritdoc />
        [ViewVariables]
        public IEntity Owner => Manager.Owner;

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("showEnts")]
        public bool ShowContents { get; set; }

        /// <summary>
        /// DO NOT CALL THIS METHOD DIRECTLY!
        /// You want <see cref="IContainerManager.MakeContainer{T}(string)" /> instead.
        /// </summary>
        protected BaseContainer() { }

        /// <inheritdoc />
        public bool Insert(IEntity toinsert)
        {
            DebugTools.Assert(!Deleted);

            //Verify we can insert into this container
            if (!CanInsert(toinsert))
                return false;

            var transform = toinsert.Transform;

            // CanInsert already checks nullability of Parent (or container forgot to call base that does)
            if (toinsert.TryGetContainerMan(out var containerManager) && !containerManager.Remove(toinsert))
                return false; // Can't remove from existing container, can't insert.

            // Attach to parent first so we can check IsInContainer more easily.
            transform.AttachParent(Owner.Transform);
            InternalInsert(toinsert);

            // This is an edge case where the parent grid is the container being inserted into, so AttachParent would not unanchor.
            if (transform.Anchored)
                transform.Anchored = false;

            // spatially move the object to the location of the container. If you don't want this functionality, the
            // calling code can save the local position before calling this function, and apply it afterwords.
            transform.LocalPosition = Vector2.Zero;

            return true;
        }

        /// <inheritdoc />
        public virtual bool CanInsert(IEntity toinsert)
        {
            DebugTools.Assert(!Deleted);

            // cannot insert into itself.
            if (Owner == toinsert)
                return false;

            // no, you can't put maps or grids into containers
            if (toinsert.HasComponent<IMapComponent>() || toinsert.HasComponent<IMapGridComponent>())
                return false;

            // Crucial, prevent circular insertion.
            return !toinsert.Transform.ContainsEntity(Owner.Transform);

            //Improvement: Traverse the entire tree to make sure we are not creating a loop.
        }

        /// <inheritdoc />
        public bool Remove(IEntity toremove)
        {
            DebugTools.Assert(!Deleted);
            DebugTools.AssertNotNull(Manager);
            DebugTools.AssertNotNull(toremove);
            DebugTools.Assert(toremove.IsValid());

            if (!CanRemove(toremove)) return false;
            InternalRemove(toremove);

            toremove.Transform.AttachParentToContainerOrGrid();
            return true;
        }

        /// <inheritdoc />
        public void ForceRemove(IEntity toRemove)
        {
            DebugTools.Assert(!Deleted);
            DebugTools.AssertNotNull(Manager);
            DebugTools.AssertNotNull(toRemove);
            DebugTools.Assert(toRemove.IsValid());

            InternalRemove(toRemove);
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

        /// <summary>
        /// Implement to store the reference in whatever form you want
        /// </summary>
        /// <param name="toinsert"></param>
        protected virtual void InternalInsert(IEntity toinsert)
        {
            DebugTools.Assert(!Deleted);

            IoCManager.Resolve<IEntityManager>().EventBus.RaiseLocalEvent(Owner.Uid, new EntInsertedIntoContainerMessage(toinsert, this));
            IoCManager.Resolve<IEntityManager>().EventBus.RaiseEvent(EventSource.Local, new UpdateContainerOcclusionMessage(toinsert));
            Manager.Dirty();
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

            IoCManager.Resolve<IEntityManager>().EventBus.RaiseLocalEvent(Owner.Uid, new EntRemovedFromContainerMessage(toremove, this));
            IoCManager.Resolve<IEntityManager>().EventBus.RaiseEvent(EventSource.Local, new UpdateContainerOcclusionMessage(toremove));
            Manager.Dirty();
        }
    }
}
