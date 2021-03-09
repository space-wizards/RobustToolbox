using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Exceptions;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects
{
    /// <summary>
    /// Manager for entities -- controls things like template loading and instantiation
    /// </summary>
    public sealed class ClientEntityManager : EntityManager, IClientEntityManager
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IComponentFactory _compFactory = default!;
#if EXCEPTION_TOLERANCE
        [Dependency] private readonly IRuntimeLog _runtimeLog = default!;
#endif

        private int _nextClientEntityUid = EntityUid.ClientUid + 1;

        public override void Startup()
        {
            base.Startup();

            if (Started)
            {
                throw new InvalidOperationException("Startup() called multiple times");
            }

            EntitySystemManager.Initialize();
            Started = true;
        }

        public List<EntityUid> ApplyEntityStates(EntityState[]? curEntStates, IEnumerable<EntityUid>? deletions,
            EntityState[]? nextEntStates)
        {
            var toApply = new Dictionary<IEntity, (EntityState?, EntityState?)>();
            var toInitialize = new List<Entity>();
            var created = new List<EntityUid>();
            deletions ??= new EntityUid[0];

            if (curEntStates != null && curEntStates.Length != 0)
            {
                foreach (var es in curEntStates)
                {
                    //Known entities
                    if (Entities.TryGetValue(es.Uid, out var entity))
                    {
                        toApply.Add(entity, (es, null));
                    }
                    else //Unknown entities
                    {
                        var metaState = (MetaDataComponentState?) es.ComponentStates
                            ?.FirstOrDefault(c => c.NetID == NetIDs.META_DATA);
                        if (metaState == null)
                        {
                            throw new InvalidOperationException($"Server sent new entity state for {es.Uid} without metadata component!");
                        }
                        var newEntity = CreateEntity(metaState.PrototypeId, es.Uid);
                        toApply.Add(newEntity, (es, null));
                        toInitialize.Add(newEntity);
                        created.Add(newEntity.Uid);
                    }
                }
            }

            if (nextEntStates != null && nextEntStates.Length != 0)
            {
                foreach (var es in nextEntStates)
                {
                    if (Entities.TryGetValue(es.Uid, out var entity))
                    {
                        if (toApply.TryGetValue(entity, out var state))
                        {
                            toApply[entity] = (state.Item1, es);
                        }
                        else
                        {
                            toApply[entity] = (null, es);
                        }
                    }
                }
            }

            // Make sure this is done after all entities have been instantiated.
            foreach (var kvStates in toApply)
            {
                var ent = kvStates.Key;
                var entity = (Entity) ent;
                HandleEntityState(entity.EntityManager.ComponentManager, entity, kvStates.Value.Item1,
                    kvStates.Value.Item2);
            }

            foreach (var id in deletions)
            {
                DeleteEntity(id);
            }

#if EXCEPTION_TOLERANCE
            HashSet<Entity> brokenEnts = new HashSet<Entity>();
#endif

            foreach (var entity in toInitialize)
            {
#if EXCEPTION_TOLERANCE
                try
                {
#endif
                    InitializeEntity(entity);
#if EXCEPTION_TOLERANCE
                }
                catch (Exception e)
                {
                    Logger.ErrorS("state", $"Server entity threw in Init: uid={entity.Uid}, proto={entity.Prototype}\n{e}");
                    brokenEnts.Add(entity);
                }
#endif
            }

            foreach (var entity in toInitialize)
            {
#if EXCEPTION_TOLERANCE
                if(brokenEnts.Contains(entity))
                    continue;

                try
                {
#endif
                    StartEntity(entity);
#if EXCEPTION_TOLERANCE
                }
                catch (Exception e)
                {
                    Logger.ErrorS("state", $"Server entity threw in Start: uid={entity.Uid}, proto={entity.Prototype}\n{e}");
                    brokenEnts.Add(entity);
                }
#endif
            }

            foreach (var entity in toInitialize)
            {
#if EXCEPTION_TOLERANCE
                if(brokenEnts.Contains(entity))
                    continue;
#endif
            }
#if EXCEPTION_TOLERANCE
            foreach (var entity in brokenEnts)
            {
                entity.Delete();
            }
#endif

            return created;
        }

        /// <inheritdoc />
        public override IEntity CreateEntityUninitialized(string? prototypeName)
        {
            return CreateEntity(prototypeName);
        }

        /// <inheritdoc />
        public override IEntity CreateEntityUninitialized(string? prototypeName, EntityCoordinates coordinates)
        {
            var newEntity = CreateEntity(prototypeName, GenerateEntityUid());

            if (TryGetEntity(coordinates.EntityId, out var entity))
            {
                newEntity.Transform.AttachParent(entity);
                newEntity.Transform.Coordinates = coordinates;
            }

            return newEntity;
        }

        /// <inheritdoc />
        public override IEntity CreateEntityUninitialized(string? prototypeName, MapCoordinates coordinates)
        {
            var newEntity = CreateEntity(prototypeName, GenerateEntityUid());
            newEntity.Transform.AttachParent(_mapManager.GetMapEntity(coordinates.MapId));
            newEntity.Transform.WorldPosition = coordinates.Position;
            return newEntity;
        }

        /// <inheritdoc />
        public override IEntity SpawnEntity(string? protoName, EntityCoordinates coordinates)
        {
            var newEnt = CreateEntityUninitialized(protoName, coordinates);
            InitializeAndStartEntity((Entity) newEnt);
            return newEnt;
        }

        /// <inheritdoc />
        public override IEntity SpawnEntity(string? protoName, MapCoordinates coordinates)
        {
            var entity = CreateEntityUninitialized(protoName, coordinates);
            InitializeAndStartEntity((Entity) entity);
            return entity;
        }

        /// <inheritdoc />
        public override IEntity SpawnEntityNoMapInit(string? protoName, EntityCoordinates coordinates)
        {
            return SpawnEntity(protoName, coordinates);
        }

        protected override EntityUid GenerateEntityUid()
        {
            return new(_nextClientEntityUid++);
        }

        private void HandleEntityState(IComponentManager compMan, IEntity entity, EntityState? curState,
            EntityState? nextState)
        {
            var compStateWork = new Dictionary<uint, (ComponentState? curState, ComponentState? nextState)>();
            var entityUid = entity.Uid;

            if (curState?.ComponentChanges != null)
            {
                foreach (var compChange in curState.ComponentChanges)
                {
                    if (compChange.Deleted)
                    {
                        if (compMan.TryGetComponent(entityUid, compChange.NetID, out var comp))
                        {
                            compMan.RemoveComponent(entityUid, comp);
                        }
                    }
                    else
                    {
                        if (compMan.HasComponent(entityUid, compChange.NetID))
                            continue;

                        var newComp = (Component) _compFactory.GetComponent(compChange.ComponentName!);
                        newComp.Owner = entity;
                        compMan.AddComponent(entity, newComp, true);
                    }
                }
            }

            if (curState?.ComponentStates != null)
            {
                foreach (var compState in curState.ComponentStates)
                {
                    compStateWork[compState.NetID] = (compState, null);
                }
            }

            if (nextState?.ComponentStates != null)
            {
                foreach (var compState in nextState.ComponentStates)
                {
                    if (compStateWork.TryGetValue(compState.NetID, out var state))
                    {
                        compStateWork[compState.NetID] = (state.curState, compState);
                    }
                    else
                    {
                        compStateWork[compState.NetID] = (null, compState);
                    }
                }
            }

            foreach (var (netId, (cur, next)) in compStateWork)
            {
                if (compMan.TryGetComponent(entityUid, netId, out var component))
                {
                    try
                    {
                        component.HandleComponentState(cur, next);
                    }
                    catch (Exception e)
                    {
                        var wrapper = new ComponentStateApplyException(
                            $"Failed to apply comp state: entity={component.Owner}, comp={component.Name}", e);
#if EXCEPTION_TOLERANCE
                    _runtimeLog.LogException(wrapper, "Component state apply");
#else
                        throw wrapper;
#endif
                    }
                }
                else
                {
                    // The component can be null here due to interp.
                    // Because the NEXT state will have a new component, but this one doesn't yet.
                    // That's fine though.
                    if (cur == null)
                    {
                        continue;
                    }

                    var eUid = entityUid;
                    var eRegisteredNetUidName = _compFactory.GetRegistration(netId).Name;
                    DebugTools.Assert(
                        $"Component does not exist for state: entUid={eUid}, expectedNetId={netId}, expectedName={eRegisteredNetUidName}");
                }
            }
        }
    }
}
