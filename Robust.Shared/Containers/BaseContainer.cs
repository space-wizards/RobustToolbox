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
        [Dependency] private readonly IEntityManager _entityManager = default!;

        /// <inheritdoc />
        [ViewVariables]
        public abstract IReadOnlyList<EntityUid> ContainedEntities { get; }

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
        public EntityUid Owner => Manager.Owner;

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("showEnts")]
        public bool ShowContents { get; set; }

        /// <summary>
        /// DO NOT CALL THIS METHOD DIRECTLY!
        /// You want <see cref="IContainerManager.MakeContainer{T}(string)" /> instead.
        /// </summary>
        protected BaseContainer()
        {
            IoCManager.InjectDependencies(this);
        }

        /// <inheritdoc />
        public bool Insert(EntityUid toinsert)
        {
            DebugTools.Assert(!Deleted);

            //Verify we can insert into this container
            if (!CanInsert(toinsert))
                return false;

            var transform = _entityManager.GetComponent<TransformComponent>(toinsert);

            // CanInsert already checks nullability of Parent (or container forgot to call base that does)
            if (toinsert.TryGetContainerMan(out var containerManager) && !containerManager.Remove(toinsert))
                return false; // Can't remove from existing container, can't insert.

            // Attach to parent first so we can check IsInContainer more easily.
            transform.AttachParent(_entityManager.GetComponent<TransformComponent>(Owner));
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
        public virtual bool CanInsert(EntityUid toinsert)
        {
            DebugTools.Assert(!Deleted);

            // cannot insert into itself.
            if (Owner == toinsert)
                return false;

            // no, you can't put maps or grids into containers
            if (_entityManager.HasComponent<IMapComponent>(toinsert) || _entityManager.HasComponent<IMapGridComponent>(toinsert))
                return false;

            // Crucial, prevent circular insertion.
            if (!_entityManager.GetComponent<TransformComponent>(toinsert)
                    .ContainsEntity(_entityManager.GetComponent<TransformComponent>(Owner)))
                return false;

            //Improvement: Traverse the entire tree to make sure we are not creating a loop.

            //raise events
            var insertAttemptEvent = new ContainerIsInsertingAttemptEvent(this, toinsert);
            _entityManager.EventBus.RaiseLocalEvent(Owner, insertAttemptEvent);
            if (insertAttemptEvent.Cancelled)
                return false;

            var gettingInsertedAttemptEvent = new ContainerGettingInsertedAttemptEvent(this, toinsert);
            _entityManager.EventBus.RaiseLocalEvent(toinsert, gettingInsertedAttemptEvent);
            if (gettingInsertedAttemptEvent.Cancelled)
                return false;

            return true;
        }

        /// <inheritdoc />
        public bool Remove(EntityUid toremove)
        {
            DebugTools.Assert(!Deleted);
            DebugTools.AssertNotNull(Manager);
            DebugTools.AssertNotNull(toremove);
            DebugTools.Assert(_entityManager.EntityExists(toremove));

            if (!CanRemove(toremove)) return false;
            InternalRemove(toremove);

            _entityManager.GetComponent<TransformComponent>(toremove).AttachParentToContainerOrGrid();
            return true;
        }

        /// <inheritdoc />
        public void ForceRemove(EntityUid toRemove)
        {
            DebugTools.Assert(!Deleted);
            DebugTools.AssertNotNull(Manager);
            DebugTools.AssertNotNull(toRemove);
            DebugTools.Assert(_entityManager.EntityExists(toRemove));

            InternalRemove(toRemove);
        }

        /// <inheritdoc />
        public virtual bool CanRemove(EntityUid toremove)
        {
            DebugTools.Assert(!Deleted);

            if (!Contains(toremove))
                return false;

            //raise events
            var removeAttemptEvent = new ContainerIsRemovingAttemptEvent(this, toremove);
            _entityManager.EventBus.RaiseLocalEvent(Owner, removeAttemptEvent);
            if (removeAttemptEvent.Cancelled)
                return false;

            var gettingRemovedAttemptEvent = new ContainerGettingRemovedAttemptEvent(this, toremove);
            _entityManager.EventBus.RaiseLocalEvent(toremove, gettingRemovedAttemptEvent);
            if (gettingRemovedAttemptEvent.Cancelled)
                return false;

            return true;
        }

        /// <inheritdoc />
        public abstract bool Contains(EntityUid contained);

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
        protected virtual void InternalInsert(EntityUid toinsert)
        {
            DebugTools.Assert(!Deleted);

            _entityManager.EventBus.RaiseLocalEvent(Owner, new EntInsertedIntoContainerMessage(toinsert, this));
            _entityManager.EventBus.RaiseEvent(EventSource.Local, new UpdateContainerOcclusionMessage(toinsert));
            Manager.Dirty();
        }

        /// <summary>
        /// Implement to remove the reference you used to store the entity
        /// </summary>
        /// <param name="toremove"></param>
        protected virtual void InternalRemove(EntityUid toremove)
        {
            DebugTools.Assert(!Deleted);
            DebugTools.AssertNotNull(Manager);
            DebugTools.AssertNotNull(toremove);
            DebugTools.Assert(_entityManager.EntityExists(toremove));

            _entityManager.EventBus.RaiseLocalEvent(Owner, new EntRemovedFromContainerMessage(toremove, this));
            _entityManager.EventBus.RaiseEvent(EventSource.Local, new UpdateContainerOcclusionMessage(toremove));
            Manager.Dirty();
        }
    }
}
