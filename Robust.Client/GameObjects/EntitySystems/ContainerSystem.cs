using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using static Robust.Shared.Containers.ContainerManagerComponent;

namespace Robust.Client.GameObjects
{
    public class ContainerSystem : SharedContainerSystem
    {
        [Dependency] private readonly IRobustSerializer _serializer = default!;
        [Dependency] private readonly IDynamicTypeFactoryInternal _dynFactory = default!;

        private readonly HashSet<IEntity> _updateQueue = new();

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
            if (!ExpectedEntities.TryGetValue(ev.Entity.Uid, out var container))
                return;

            RemoveExpectedEntity(ev.Entity.Uid);

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
                List<IEntity>? toRemove = null;
                foreach (var entity in container.ContainedEntities)
                {
                    if (!entityUids.Contains(entity.Uid))
                    {
                        toRemove ??= new List<IEntity>();
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
                foreach (var entityUid in entityUids)
                {
                    if (!EntityManager.TryGetEntity(entityUid, out var entity))
                    {
                        AddExpectedEntity(entityUid, container);
                        continue;
                    }

                    if (!container.ContainedEntities.Contains(entity))
                        container.Insert(entity);
                }
            }
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

            foreach (var toUpdate in _updateQueue)
            {
                if (toUpdate.Deleted)
                {
                    continue;
                }

                UpdateEntityRecursively(toUpdate);
            }

            _updateQueue.Clear();
        }

        private static void UpdateEntityRecursively(IEntity entity)
        {
            // TODO: Since we are recursing down,
            // we could cache ShowContents data here to speed it up for children.
            // Am lazy though.
            UpdateEntity(entity);

            foreach (var child in entity.Transform.Children)
            {
                UpdateEntityRecursively(child.Owner);
            }
        }

        private static void UpdateEntity(IEntity entity)
        {
            if (entity.TryGetComponent(out SpriteComponent? sprite))
            {
                sprite.ContainerOccluded = false;

                // We have to recursively scan for containers upwards in case of nested containers.
                var tempParent = entity;
                while (tempParent.TryGetContainer(out var container))
                {
                    if (!container.ShowContents)
                    {
                        sprite.ContainerOccluded = true;
                        break;
                    }

                    tempParent = container.Owner;
                }
            }

            if (entity.TryGetComponent(out PointLightComponent? light))
            {
                light.ContainerOccluded = false;

                // We have to recursively scan for containers upwards in case of nested containers.
                var tempParent = entity;
                while (tempParent.TryGetContainer(out var container))
                {
                    if (container.OccludesLight)
                    {
                        light.ContainerOccluded = true;
                        break;
                    }

                    tempParent = container.Owner;
                }
            }
        }
    }
}
