using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.GameStates
{
    internal partial class PVSSystem
    {
        /// <summary>
        /// Keep around a cache of players who don't need every entity state dumped on them where culling disabled.
        /// </summary>
        private HashSet<ICommonSession> _oldPlayers = new();

        /// <summary>
        /// Generates a network entity state for the given entity.
        /// </summary>
        /// <param name="player">The player to generate this state for.</param>
        /// <param name="entityUid">Uid of the entity to generate the state from.</param>
        /// <param name="fromTick">Only provide delta changes from this tick.</param>
        /// <returns>New entity State for the given entity.</returns>
        private EntityState GetEntityState(ICommonSession player, EntityUid entityUid, GameTick fromTick)
        {
            var bus = _entMan.EventBus;
            var changed = new List<ComponentChange>();

            foreach (var (netId, component) in _entMan.GetNetComponents(entityUid))
            {
                DebugTools.Assert(component.Initialized);

                // NOTE: When LastModifiedTick or CreationTick are 0 it means that the relevant data is
                // "not different from entity creation".
                // i.e. when the client spawns the entity and loads the entity prototype,
                // the data it deserializes from the prototype SHOULD be equal
                // to what the component state / ComponentChange would send.
                // As such, we can avoid sending this data in this case since the client "already has it".

                DebugTools.Assert(component.LastModifiedTick >= component.CreationTick);

                if (component.CreationTick != GameTick.Zero && component.CreationTick >= fromTick && !component.Deleted)
                {
                    ComponentState? state = null;
                    if (component.NetSyncEnabled && component.LastModifiedTick != GameTick.Zero && component.LastModifiedTick >= fromTick)
                        state = _entMan.GetComponentState(bus, component, player);

                    // Can't be null since it's returned by GetNetComponents
                    // ReSharper disable once PossibleInvalidOperationException
                    changed.Add(ComponentChange.Added(netId, state));
                }
                else if (component.NetSyncEnabled && component.LastModifiedTick != GameTick.Zero && component.LastModifiedTick >= fromTick)
                {
                    changed.Add(ComponentChange.Changed(netId, _entMan.GetComponentState(bus, component, player)));
                }
            }

            foreach (var netId in _entMan.GetDeletedComponents(entityUid, fromTick))
            {
                changed.Add(ComponentChange.Removed(netId));
            }

            return new EntityState(entityUid, changed.ToArray());
        }

        /// <summary>
        ///     Gets all entity states that have been modified after and including the provided tick.
        /// </summary>
        private List<EntityState>? GetAllEntityStates(ICommonSession player, GameTick fromTick, GameTick toTick)
        {
            List<EntityState> stateEntities;

            if (_oldPlayers.Contains(player))
            {
                stateEntities = new List<EntityState>();
                var seenEnts = new HashSet<EntityUid>();
                var slowPath = false;

                for (var i = fromTick.Value; i <= toTick.Value; i++)
                {
                    var tick = new GameTick(i);
                    if (!TryGetTick(tick, out var add, out var dirty))
                    {
                        slowPath = true;
                        break;
                    }

                    foreach (var uid in add)
                    {
                        if (!seenEnts.Add(uid) || !_entMan.TryGetEntity(uid, out var entity) || entity.Deleted) continue;

                        DebugTools.Assert(entity.Initialized);

                        if (entity.LastModifiedTick >= fromTick)
                            stateEntities.Add(GetEntityState(player, entity.Uid, GameTick.Zero));
                    }

                    foreach (var uid in dirty)
                    {
                        DebugTools.Assert(!add.Contains(uid));

                        if (!seenEnts.Add(uid) || !_entMan.TryGetEntity(uid, out var entity) || entity.Deleted) continue;

                        DebugTools.Assert(entity.Initialized);

                        if (entity.LastModifiedTick >= fromTick)
                            stateEntities.Add(GetEntityState(player, entity.Uid, fromTick));
                    }
                }

                if (!slowPath)
                {
                    return stateEntities.Count == 0 ? default : stateEntities;
                }
            }

            stateEntities = new List<EntityState>(EntityManager.EntityCount);

            foreach (var entity in _entMan.GetEntities())
            {
                if (entity.Deleted)
                {
                    continue;
                }

                DebugTools.Assert(entity.Initialized);

                if (entity.LastModifiedTick >= fromTick)
                    stateEntities.Add(GetEntityState(player, entity.Uid, fromTick));
            }

            // no point sending an empty collection
            return stateEntities.Count == 0 ? default : stateEntities;
        }
    }
}
