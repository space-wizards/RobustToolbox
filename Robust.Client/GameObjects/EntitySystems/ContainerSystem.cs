using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using static Robust.Shared.Containers.ContainerManagerComponent;

namespace Robust.Client.GameObjects
{
    public sealed class ContainerSystem : SharedContainerSystem
    {
        [Dependency] private readonly IRobustSerializer _serializer = default!;
        [Dependency] private readonly IDynamicTypeFactoryInternal _dynFactory = default!;

        private readonly HashSet<EntityUid> _updateQueue = new();

        public readonly Dictionary<EntityUid, IContainer> ExpectedEntities = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<UpdateContainerOcclusionMessage>(UpdateContainerOcclusion);
            SubscribeLocalEvent<EntityInitializedMessage>(HandleEntityInitialized);
            SubscribeLocalEvent<ContainerManagerComponent, ComponentHandleState>(HandleComponentState);

            UpdatesBefore.Add(typeof(SpriteSystem));
        }

        private void UpdateContainerOcclusion(UpdateContainerOcclusionMessage ev)
        {
            _updateQueue.Add(ev.Entity);
        }

        private void HandleEntityInitialized(EntityInitializedMessage ev)
        {
            if (!ExpectedEntities.TryGetValue(ev.Entity, out var container))
                return;

            RemoveExpectedEntity(ev.Entity);

            if (container.Deleted)
                return;

            container.Insert(ev.Entity);
        }

        private void HandleComponentState(EntityUid uid, ContainerManagerComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not ContainerManagerComponentState cast)
                return;

            // Delete now-gone containers.
            List<string>? toDelete = null;
            foreach (var (id, container) in component.Containers)
            {
                if (!cast.ContainerSet.Any(data => data.Id == id))
                {
                    container.EmptyContainer(true, entMan: EntityManager);
                    container.Shutdown();
                    toDelete ??= new List<string>();
                    toDelete.Add(id);
                }
            }

            if (toDelete != null)
            {
                foreach (var dead in toDelete)
                {
                    component.Containers.Remove(dead);
                }
            }

            // Add new containers and update existing contents.

            foreach (var (containerType, id, showEnts, occludesLight, entityUids) in cast.ContainerSet)
            {
                if (!component.Containers.TryGetValue(id, out var container))
                {
                    container = ContainerFactory(component, containerType, id);
                    component.Containers.Add(id, container);
                }

                // sync show flag
                container.ShowContents = showEnts;
                container.OccludesLight = occludesLight;

                // Remove gone entities.
                List<EntityUid>? toRemove = null;
                foreach (var entity in container.ContainedEntities)
                {
                    if (!entityUids.Contains(entity))
                    {
                        toRemove ??= new List<EntityUid>();
                        toRemove.Add(entity);
                    }
                }

                if (toRemove != null)
                {
                    foreach (var goner in toRemove)
                        container.Remove(goner);
                }

                // Remove entities that were expected, but have been removed from the container.
                List<EntityUid>? removedExpected = null;
                foreach (var entityUid in container.ExpectedEntities)
                {
                    if (!entityUids.Contains(entityUid))
                    {
                        removedExpected ??= new List<EntityUid>();
                        removedExpected.Add(entityUid);
                    }
                }

                if (removedExpected != null)
                {
                    foreach (var entityUid in removedExpected)
                        RemoveExpectedEntity(entityUid);
                }

                // Add new entities.
                foreach (var entity in entityUids)
                {
                    if (!EntityManager.EntityExists(entity))
                    {
                        AddExpectedEntity(entity, container);
                        continue;
                    }

                    // If an entity is currently in the shadow realm, it means we probably left PVS and are now getting
                    // back into range. We do not want to directly insert this entity, as IF the container and entity
                    // transform states did not get sent simultaneously, the entity's transform will be modified by the
                    // insert operation. This means it will then be reset to the shadow realm, causing it to be ejected
                    // from the container. It would then subsequently be parented to the container without ever being
                    // re-inserted, leading to the client seeing what should be hidden entities attached to
                    // containers/players.
                    if (Transform(entity).MapID == MapId.Nullspace)
                    {
                        AddExpectedEntity(entity, container);
                        continue;
                    }

                    if (!container.ContainedEntities.Contains(entity))
                        container.Insert(entity);
                }
            }
        }

        protected override void HandleParentChanged(ref EntParentChangedMessage message)
        {
            base.HandleParentChanged(ref message);

            // If an entity warped in from null-space (i.e., re-entered PVS) and got attached to a container, do the same checks as for newly initialized entities.
            if (message.OldParent != null && message.OldParent.Value.IsValid())
                return;

            if (!ExpectedEntities.TryGetValue(message.Entity, out var container))
                return;

            if (Transform(message.Entity).ParentUid != container.Owner)
            {
                // This container is expecting an entity... but it got parented to some other entity???
                // Ah well, the sever should send a new container state that updates expected entities so just ignore it for now.
                return;
            }

            RemoveExpectedEntity(message.Entity);

            if (container.Deleted)
                return;

            container.Insert(message.Entity);
        }

        private IContainer ContainerFactory(ContainerManagerComponent component, string containerType, string id)
        {
            var type = _serializer.FindSerializedType(typeof(IContainer), containerType);
            if (type is null) throw new ArgumentException($"Container of type {containerType} for id {id} cannot be found.");

            var newContainer = _dynFactory.CreateInstanceUnchecked<BaseContainer>(type);
            newContainer.ID = id;
            newContainer.Manager = component;
            return newContainer;
        }

        public void AddExpectedEntity(EntityUid uid, IContainer container)
        {
            if (ExpectedEntities.ContainsKey(uid))
                return;

            ExpectedEntities.Add(uid, container);
            container.ExpectedEntities.Add(uid);
        }

        public void RemoveExpectedEntity(EntityUid uid)
        {
            if (!ExpectedEntities.TryGetValue(uid, out var container))
                return;

            ExpectedEntities.Remove(uid);
            container.ExpectedEntities.Remove(uid);
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);
            var pointQuery = EntityManager.GetEntityQuery<PointLightComponent>();
            var spriteQuery = EntityManager.GetEntityQuery<SpriteComponent>();
            var xformQuery = EntityManager.GetEntityQuery<TransformComponent>();

            foreach (var toUpdate in _updateQueue)
            {
                if (Deleted(toUpdate))
                    continue;

                UpdateEntityRecursively(toUpdate, xformQuery, pointQuery, spriteQuery);
            }

            _updateQueue.Clear();
        }

        private void UpdateEntityRecursively(
            EntityUid entity,
            EntityQuery<TransformComponent> xformQuery,
            EntityQuery<PointLightComponent> pointQuery,
            EntityQuery<SpriteComponent> spriteQuery)
        {
            // Recursively go up parents and containers to see whether both sprites and lights need to be occluded
            // Could maybe optimise this more by checking nearest parent that has sprite / light and whether it's container
            // occluded but this probably isn't a big perf issue.
            var xform = xformQuery.GetComponent(entity);
            var parent = xform.ParentUid;
            var child = entity;
            var spriteOccluded = false;
            var lightOccluded = false;

            while (parent.IsValid() && !spriteOccluded && !lightOccluded)
            {
                var parentXform = xformQuery.GetComponent(parent);
                if (TryComp<ContainerManagerComponent>(parent, out var manager) && manager.TryGetContainer(child, out var container))
                {
                    spriteOccluded = spriteOccluded || !container.ShowContents;
                    lightOccluded = lightOccluded || container.OccludesLight;
                }

                child = parent;
                parent = parentXform.ParentUid;
            }

            // Alright so
            // This is the CBT bit.
            // The issue is we need to go through the children and re-check whether they are or are not contained.
            // if they are contained then the occlusion values may need updating for all those children
            UpdateEntity(entity, xform, xformQuery, pointQuery, spriteQuery, spriteOccluded, lightOccluded);
        }

        private void UpdateEntity(
            EntityUid entity,
            TransformComponent xform,
            EntityQuery<TransformComponent> xformQuery,
            EntityQuery<PointLightComponent> pointQuery,
            EntityQuery<SpriteComponent> spriteQuery,
            bool spriteOccluded,
            bool lightOccluded)
        {
            if (spriteQuery.TryGetComponent(entity, out var sprite))
            {
                sprite.ContainerOccluded = spriteOccluded;
            }

            if (pointQuery.TryGetComponent(entity, out var light))
            {
                light.ContainerOccluded = lightOccluded;
            }

            var childEnumerator = xform.ChildEnumerator;

            // Try to avoid TryComp if we already know stuff is occluded.
            if ((!spriteOccluded || !lightOccluded) && TryComp<ContainerManagerComponent>(entity, out var manager))
            {
                while (childEnumerator.MoveNext(out var child))
                {
                    // Thank god it's by value and not by ref.
                    var childSpriteOccluded = spriteOccluded;
                    var childLightOccluded = lightOccluded;

                    // We already know either sprite or light is not occluding so need to check container.
                    if (manager.TryGetContainer(child.Value, out var container))
                    {
                        childSpriteOccluded = childSpriteOccluded || !container.ShowContents;
                        childLightOccluded = childLightOccluded || container.OccludesLight;
                    }

                    UpdateEntity(child.Value, xformQuery.GetComponent(child.Value), xformQuery, pointQuery, spriteQuery, childSpriteOccluded, childLightOccluded);
                }
            }
            else
            {
                while (childEnumerator.MoveNext(out var child))
                {
                    UpdateEntity(child.Value, xformQuery.GetComponent(child.Value), xformQuery, pointQuery, spriteQuery, spriteOccluded, lightOccluded);
                }
            }
        }
    }
}
