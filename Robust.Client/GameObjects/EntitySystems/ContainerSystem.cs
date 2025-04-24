using System;
using Robust.Shared.Collections;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Utility;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Serialization;
using static Robust.Shared.Containers.ContainerManagerComponent;

namespace Robust.Client.GameObjects
{
    public sealed class ContainerSystem : SharedContainerSystem
    {
        [Dependency] private readonly IRobustSerializer _serializer = default!;
        [Dependency] private readonly IDynamicTypeFactoryInternal _dynFactory = default!;
        [Dependency] private readonly PointLightSystem _lightSys = default!;

        private EntityQuery<PointLightComponent> _pointLightQuery;
        private EntityQuery<SpriteComponent> _spriteQuery;

        private readonly HashSet<EntityUid> _updateQueue = new();

        public readonly Dictionary<NetEntity, BaseContainer> ExpectedEntities = new();

        public override void Initialize()
        {
            base.Initialize();

            _pointLightQuery = GetEntityQuery<PointLightComponent>();
            _spriteQuery = GetEntityQuery<SpriteComponent>();

            EntityManager.EntityInitialized += HandleEntityInitialized;
            SubscribeLocalEvent<ContainerManagerComponent, ComponentHandleState>(HandleComponentState);

            UpdatesBefore.Add(typeof(SpriteSystem));
        }

        public override void Shutdown()
        {
            EntityManager.EntityInitialized -= HandleEntityInitialized;
            base.Shutdown();
        }

        protected override void ValidateMissingEntity(EntityUid uid, BaseContainer cont, EntityUid missing)
        {
            var netEntity = GetNetEntity(missing);
            DebugTools.Assert(ExpectedEntities.TryGetValue(netEntity, out var expectedContainer) && expectedContainer == cont && cont.ExpectedEntities.Contains(netEntity));
        }

        private void HandleEntityInitialized(Entity<MetaDataComponent> ent)
        {
            var (uid, meta) = ent;
            if (!RemoveExpectedEntity(meta.NetEntity, out var container))
                return;

            Insert((uid, TransformQuery.GetComponent(uid), MetaQuery.GetComponent(uid), null), container);
        }

        public override void ShutdownContainer(BaseContainer container)
        {
            foreach (var ent in container.ExpectedEntities)
            {
                if (ExpectedEntities.Remove(ent, out var c))
                    DebugTools.Assert(c == container);
            }

            base.ShutdownContainer(container);
        }

        private void HandleComponentState(EntityUid uid, ContainerManagerComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not ContainerManagerComponentState cast)
                return;

            var xform = TransformQuery.GetComponent(uid);

            // Delete now-gone containers.
            var toDelete = new ValueList<string>();
            foreach (var (id, container) in component.Containers)
            {
                if (cast.Containers.ContainsKey(id))
                {
                    DebugTools.Assert(cast.Containers[id].ContainerType == container.GetType().Name);
                    continue;
                }

                foreach (var entity in container.ContainedEntities.ToArray())
                {
                    Remove(
                        (entity, TransformQuery.GetComponent(entity), MetaQuery.GetComponent(entity)),
                        container,
                        force: true,
                        reparent: false
                    );

                    DebugTools.Assert(!container.Contains(entity));
                }

                ShutdownContainer(container);
                toDelete.Add(id);
            }

            foreach (var dead in toDelete.Span)
            {
                component.Containers.Remove(dead);
            }

            // Add new containers and update existing contents.

            foreach (var (id, data) in cast.Containers)
            {
                if (!component.Containers.TryGetValue(id, out var container))
                {
                    var type = _serializer.FindSerializedType(typeof(BaseContainer), data.ContainerType);
                    container = _dynFactory.CreateInstanceUnchecked<BaseContainer>(type!, inject:false);
                    container.Init(this, id, (uid, component));
                    component.Containers.Add(id, container);
                }

                DebugTools.Assert(container.ID == id);
                container.ShowContents = data.ShowContents;
                container.OccludesLight = data.OccludesLight;

                // Remove gone entities.
                var toRemove = new ValueList<EntityUid>();

                DebugTools.Assert(!container.Contains(EntityUid.Invalid));

                var stateNetEnts = data.ContainedEntities;
                var stateEnts = GetEntityArray(stateNetEnts); // No need to ensure entities.

                foreach (var entity in container.ContainedEntities)
                {
                    if (!stateEnts.Contains(entity))
                        toRemove.Add(entity);
                }

                foreach (var entity in toRemove.Span)
                {
                    Remove(
                        (entity, TransformQuery.GetComponent(entity), MetaQuery.GetComponent(entity)),
                        container,
                        force: true,
                        reparent: false
                    );

                    DebugTools.Assert(!container.Contains(entity));
                }

                // Remove entities that were expected, but have been removed from the container.
                var removedExpected = new ValueList<NetEntity>();
                foreach (var netEntity in container.ExpectedEntities)
                {
                    if (!stateNetEnts.Contains(netEntity))
                        removedExpected.Add(netEntity);
                }

                foreach (var entityUid in removedExpected.Span)
                {
                    RemoveExpectedEntity(entityUid, out _);
                }

                // Add new entities.
                for (var i = 0; i < stateNetEnts.Length; i++)
                {
                    var entity = stateEnts[i];
                    var netEnt = stateNetEnts[i];
                    if (!entity.IsValid())
                    {
                        DebugTools.Assert(netEnt.IsValid());
                        AddExpectedEntity(netEnt, container);
                        continue;
                    }

                    var meta = MetaData(entity);
                    DebugTools.Assert(meta.NetEntity == netEnt);

                    // If an entity is currently in the shadow realm, it means we probably left PVS and are now getting
                    // back into range. We do not want to directly insert this entity, as IF the container and entity
                    // transform states did not get sent simultaneously, the entity's transform will be modified by the
                    // insert operation. This means it will then be reset to the shadow realm, causing it to be ejected
                    // from the container. It would then subsequently be parented to the container without ever being
                    // re-inserted, leading to the client seeing what should be hidden entities attached to
                    // containers/players.
                    if ((meta.Flags & MetaDataFlags.Detached) != 0)
                    {
                        AddExpectedEntity(netEnt, container);
                        continue;
                    }

                    if (container.Contains(entity))
                        continue;

                    RemoveExpectedEntity(netEnt, out _);
                    Insert(
                        (entity, TransformQuery.GetComponent(entity), MetaQuery.GetComponent(entity), null),
                        container,
                        xform,
                        force: true
                    );

                    DebugTools.Assert(container.Contains(entity));
                }
            }
        }

        protected override void OnParentChanged(ref EntParentChangedMessage message)
        {
            base.OnParentChanged(ref message);

            var xform = message.Transform;

            if (xform.MapID != MapId.Nullspace)
                _updateQueue.Add(message.Entity);

            // If an entity warped in from null-space (i.e., re-entered PVS) and got attached to a container, do the same checks as for newly initialized entities.
            if (message.OldParent != null && message.OldParent.Value.IsValid())
                return;

            if (!RemoveExpectedEntity(GetNetEntity(message.Entity), out var container))
                return;

            if (xform.ParentUid != container.Owner)
            {
                // This container is expecting an entity... but it got parented to some other entity???
                // Ah well, the sever should send a new container state that updates expected entities so just ignore it for now.
                return;
            }

            Insert(message.Entity, container);
        }

        public void AddExpectedEntity(NetEntity netEntity, BaseContainer container)
        {
#if DEBUG
            var uid = GetEntity(netEntity);

            if (TryComp(uid, out MetaDataComponent? meta))
            {
                DebugTools.Assert((meta.Flags & ( MetaDataFlags.Detached | MetaDataFlags.InContainer) ) == MetaDataFlags.Detached,
                    $"Adding entity {ToPrettyString(uid)} to list of expected entities for container {container.ID} in {ToPrettyString(container.Owner)}, despite it already being in a container.");
            }
#endif

            if (!ExpectedEntities.TryAdd(netEntity, container))
            {
                // It is possible that we were expecting this entity in one container, but it has now moved to another
                // container, and this entity's state is just being applied before the old container is getting updated.
                var oldContainer = ExpectedEntities[netEntity];
                ExpectedEntities[netEntity] = container;
                DebugTools.Assert(oldContainer.ExpectedEntities.Contains(netEntity),
                    $"Entity {netEntity} is expected, but not expected in the given container? Container: {oldContainer.ID} in {ToPrettyString(oldContainer.Owner)}");
                oldContainer.ExpectedEntities.Remove(netEntity);
            }

            DebugTools.Assert(!container.ExpectedEntities.Contains(netEntity),
                $"Contained entity {netEntity} was not yet expected by the system, but was already expected by the container: {container.ID} in {ToPrettyString(container.Owner)}");
            container.ExpectedEntities.Add(netEntity);
        }

        public bool RemoveExpectedEntity(NetEntity netEntity, [NotNullWhen(true)] out BaseContainer? container)
        {
            if (!ExpectedEntities.Remove(netEntity, out container))
                return false;

            DebugTools.Assert(container.ExpectedEntities.Contains(netEntity),
                $"While removing expected contained entity {ToPrettyString(netEntity)}, the entity was missing from the container expected set. Container: {container.ID} in {ToPrettyString(container.Owner)}");
            container.ExpectedEntities.Remove(netEntity);
            return true;
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);

            foreach (var toUpdate in _updateQueue)
            {
                if (Deleted(toUpdate))
                    continue;

                UpdateEntityRecursively(toUpdate);
            }

            _updateQueue.Clear();
        }

        private void UpdateEntityRecursively(EntityUid entity)
        {
            // Recursively go up parents and containers to see whether both sprites and lights need to be occluded
            // Could maybe optimise this more by checking nearest parent that has sprite / light and whether it's container
            // occluded but this probably isn't a big perf issue.
            var xform = TransformQuery.GetComponent(entity);
            var parent = xform.ParentUid;
            var child = entity;
            var spriteOccluded = false;
            var lightOccluded = false;

            while (parent.IsValid() && (!spriteOccluded || !lightOccluded))
            {
                var parentXform = TransformQuery.GetComponent(parent);
                if (TryComp<ContainerManagerComponent>(parent, out var manager) && TryGetContainingContainer(parent, child, out var container, manager))
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
            UpdateEntity(entity, xform, spriteOccluded, lightOccluded);
        }

        private void UpdateEntity(
            EntityUid entity,
            TransformComponent xform,
            bool spriteOccluded,
            bool lightOccluded)
        {
            if (_spriteQuery.TryGetComponent(entity, out var sprite))
            {
                sprite.ContainerOccluded = spriteOccluded;
            }

            if (_pointLightQuery.TryGetComponent(entity, out var light))
                _lightSys.SetContainerOccluded(entity, lightOccluded, light);

            // Try to avoid TryComp if we already know stuff is occluded.
            if ((!spriteOccluded || !lightOccluded) && TryComp<ContainerManagerComponent>(entity, out var manager))
            {
                foreach (var child in xform._children)
                {
                    // Thank god it's by value and not by ref.
                    var childSpriteOccluded = spriteOccluded;
                    var childLightOccluded = lightOccluded;

                    // We already know either sprite or light is not occluding so need to check container.
                    if (TryGetContainingContainer(entity, child, out var container, manager))
                    {
                        childSpriteOccluded = childSpriteOccluded || !container.ShowContents;
                        childLightOccluded = childLightOccluded || container.OccludesLight;
                    }

                    UpdateEntity(child, TransformQuery.GetComponent(child), childSpriteOccluded, childLightOccluded);
                }
            }
            else
            {
                foreach (var child in xform._children)
                {
                    UpdateEntity(child, TransformQuery.GetComponent(child), spriteOccluded, lightOccluded);
                }
            }
        }
    }
}
