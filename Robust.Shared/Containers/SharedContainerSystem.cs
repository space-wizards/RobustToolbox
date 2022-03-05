using System;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Containers
{
    public abstract class SharedContainerSystem : EntitySystem
    {
        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EntParentChangedMessage>(HandleParentChanged);
        }

        // TODO: Make ContainerManagerComponent ECS and make these proxy methods the real deal.

        #region Proxy Methods

        public T MakeContainer<T>(EntityUid uid, string id, ContainerManagerComponent? containerManager = null)
            where T : IContainer
        {
            if (!Resolve(uid, ref containerManager, false))
                containerManager = EntityManager.AddComponent<ContainerManagerComponent>(uid); // Happy Vera.

            return containerManager.MakeContainer<T>(id);
        }

        public T EnsureContainer<T>(EntityUid uid, string id, out bool alreadyExisted, ContainerManagerComponent? containerManager = null)
            where T : IContainer
        {
            if (!Resolve(uid, ref containerManager, false))
                containerManager = EntityManager.AddComponent<ContainerManagerComponent>(uid);

            if (TryGetContainer(uid, id, out var container, containerManager))
            {
                alreadyExisted = true;
                if (container is T cast)
                    return cast;

                throw new InvalidOperationException(
                    $"The container exists but is of a different type: {container.GetType()}");
            }

            alreadyExisted = false;
            return MakeContainer<T>(uid, id, containerManager);
        }

        public T EnsureContainer<T>(EntityUid uid, string id, ContainerManagerComponent? containerManager = null)
           where T : IContainer
        {
            return EnsureContainer<T>(uid, id, out _, containerManager);
        }

        public IContainer GetContainer(EntityUid uid, string id, ContainerManagerComponent? containerManager = null)
        {
            if (!Resolve(uid, ref containerManager))
                throw new ArgumentException("Entity does not have a ContainerManagerComponent!", nameof(uid));

            return containerManager.GetContainer(id);
        }

        public bool HasContainer(EntityUid uid, string id, ContainerManagerComponent? containerManager)
        {
            if (!Resolve(uid, ref containerManager, false))
                return false;

            return containerManager.HasContainer(id);
        }

        public bool TryGetContainer(EntityUid uid, string id, [NotNullWhen(true)] out IContainer? container, ContainerManagerComponent? containerManager = null)
        {
            if (Resolve(uid, ref containerManager, false))
                return containerManager.TryGetContainer(id, out container);

            container = null;
            return false;
        }

        public bool TryGetContainingContainer(EntityUid uid, EntityUid containedUid, [NotNullWhen(true)] out IContainer? container, ContainerManagerComponent? containerManager = null, bool skipExistCheck = false)
        {
            if (Resolve(uid, ref containerManager, false) && (skipExistCheck || EntityManager.EntityExists(containedUid)))
                return containerManager.TryGetContainer(containedUid, out container);

            container = null;
            return false;
        }

        public bool ContainsEntity(EntityUid uid, EntityUid containedUid, ContainerManagerComponent? containerManager = null)
        {
            if (!Resolve(uid, ref containerManager, false) || !EntityManager.EntityExists(containedUid))
                return false;

            return containerManager.ContainsEntity(containedUid);
        }

        public void RemoveEntity(EntityUid uid, EntityUid containedUid, bool force = false, ContainerManagerComponent? containerManager = null)
        {
            if (!Resolve(uid, ref containerManager) || !EntityManager.EntityExists(containedUid))
                return;

            if (force)
                containerManager.ForceRemove(containedUid);
            else
                containerManager.Remove(containedUid);
        }

        public ContainerManagerComponent.AllContainersEnumerable GetAllContainers(EntityUid uid, ContainerManagerComponent? containerManager = null)
        {
            if (!Resolve(uid, ref containerManager))
                return new ContainerManagerComponent.AllContainersEnumerable();

            return containerManager.GetAllContainers();
        }

        #endregion

        #region Container Helpers

        public bool TryGetContainingContainer(EntityUid uid, [NotNullWhen(true)] out IContainer? container, TransformComponent? transform = null)
        {
            container = null;
            if (!Resolve(uid, ref transform, false))
                return false;

            if (!transform.IsInContainer)
                return false;

            return TryGetContainingContainer(transform.ParentUid, uid, out container, skipExistCheck: true);
        }

        public bool IsEntityInContainer(EntityUid uid, TransformComponent? transform = null)
        {
            if (!Resolve(uid, ref transform, false))
                return false;

            return transform.IsInContainer;
        }

        /// <summary>
        ///     Returns true if the two entities are not contained, or are contained in the same container.
        /// </summary>
        public bool IsInSameOrNoContainer(EntityUid user, EntityUid other)
        {
            var isUserContained = TryGetContainingContainer(user, out var userContainer);
            var isOtherContained = TryGetContainingContainer(other, out var otherContainer);

            // Both entities are not in a container
            if (!isUserContained && !isOtherContained) return true;

            // Both entities are in different contained states
            if (isUserContained != isOtherContained) return false;

            // Both entities are in the same container
            return userContainer == otherContainer;
        }

        /// <summary>
        ///     Returns true if the two entities are not contained, or are contained in the same container, or if one
        ///     entity contains the other (i.e., is the parent).
        /// </summary>
        public bool IsInSameOrParentContainer(EntityUid user, EntityUid other)
        {
            var isUserContained = TryGetContainingContainer(user, out var userContainer);
            var isOtherContained = TryGetContainingContainer(other, out var otherContainer);

            // Both entities are not in a container
            if (!isUserContained && !isOtherContained) return true;

            // One contains the other
            if (userContainer?.Owner == other || otherContainer?.Owner == user) return true;

            // Both entities are in different contained states
            if (isUserContained != isOtherContained) return false;

            // Both entities are in the same container
            return userContainer == otherContainer;
        }

        /// <summary>
        ///     Check whether a given entity can see another entity despite whatever containers they may be in.
        /// </summary>
        /// <remarks>
        ///     This is effectively a variant of <see cref="IsInSameOrParentContainer"/> that also checks whether the
        ///     containers are transparent. Additionally, an entity can "see" the entity that contains it, but unless
        ///     otherwise specified the containing entity cannot see into itself. For example, a human in a locker can
        ///     see the locker and other items in that locker, but the human cannot see their own organs.  Note that
        ///     this means that the two entity arguments are NOT interchangeable.
        /// </remarks>
        public bool IsInSameOrTransparentContainer(
            EntityUid user,
            EntityUid other,
            IContainer? userContainer = null,
            IContainer? otherContainer = null,
            bool userSeeInsideSelf = false)
        {
            if (userContainer == null)
                TryGetContainingContainer(user, out userContainer);

            if (otherContainer == null)
                TryGetContainingContainer(other, out otherContainer);

            // Are both entities in the same container (or none)?
            if (userContainer == otherContainer) return true;

            // Is the user contained in the other entity?
            if (userContainer?.Owner == other) return true;

            // Does the user contain the other and can they see through themselves?
            if (userSeeInsideSelf && otherContainer?.Owner == user) return true;

            // Next we check for see-through containers. This uses some recursion, but it should be fine unless people
            // start spawning in glass matryoshka dolls.

            // Is the user in a see-through container?
            if (userContainer?.ShowContents ?? false)
                return IsInSameOrTransparentContainer(userContainer.Owner, other, otherContainer: otherContainer);

            // Is the other entity in a see-through container?
            if (otherContainer?.ShowContents ?? false)
                return IsInSameOrTransparentContainer(user, otherContainer.Owner, userContainer: userContainer, userSeeInsideSelf: userSeeInsideSelf);

            return false;
        }

        /// <summary>
        /// Gets the top-most container in the hierarchy for this entity, if it exists.
        /// </summary>
        public bool TryGetOuterContainer(EntityUid uid, TransformComponent xform, [NotNullWhen(true)] out IContainer? container)
        {
            var xformQuery = EntityManager.GetEntityQuery<TransformComponent>();
            return TryGetOuterContainer(uid, xform, out container, xformQuery);
        }

        public bool TryGetOuterContainer(EntityUid uid, TransformComponent xform,
            [NotNullWhen(true)] out IContainer? container, EntityQuery<TransformComponent> xformQuery)
        {
            container = null;

            if (!uid.IsValid())
                return false;

            var conQuery = EntityManager.GetEntityQuery<ContainerManagerComponent>();
            var child = uid;
            var parent = xform.ParentUid;

            while (parent.IsValid())
            {
                if (conQuery.TryGetComponent(parent, out var conManager) &&
                    conManager.TryGetContainer(child, out var parentContainer))
                {
                    container = parentContainer;
                }

                var parentXform = xformQuery.GetComponent(parent);
                child = parent;
                parent = parentXform.ParentUid;
            }

            return container != null;
        }

        #endregion

        // Eject entities from their parent container if the parent change is done by the transform only.
        protected virtual void HandleParentChanged(ref EntParentChangedMessage message)
        {
            var oldParentEntity = message.OldParent;

            if (oldParentEntity == null || !EntityManager.EntityExists(oldParentEntity!.Value))
                return;

            if (EntityManager.TryGetComponent(oldParentEntity!.Value, out IContainerManager? containerManager))
                containerManager.ForceRemove(message.Entity);
        }
    }
}
