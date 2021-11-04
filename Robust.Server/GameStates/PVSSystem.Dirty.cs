using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Robust.Server.GameStates
{
    /// <summary>
    /// Caching for dirty bodies
    /// </summary>
    internal partial class PVSSystem
    {
        private const int DirtyBufferSize = 4;

        /// <summary>
        /// if it's a new entity we need to GetEntityState from tick 0.
        /// </summary>
        private HashSet<EntityUid>[] _addEntities = new HashSet<EntityUid>[DirtyBufferSize];
        private HashSet<EntityUid>[] _dirtyEntities = new HashSet<EntityUid>[DirtyBufferSize];
        private int _currentIndex = 1;

        private void InitializeDirty()
        {
            for (var i = 0; i < DirtyBufferSize; i++)
            {
                _addEntities[i] = new HashSet<EntityUid>(32);
                _dirtyEntities[i] = new HashSet<EntityUid>(32);
            }
        }

        private void OnEntityAdd(object? sender, EntityUid e)
        {
            _addEntities[_currentIndex].Add(e);
        }

        private void OnDirty(ref EntityDirtyEvent ev)
        {
            if (_addEntities[_currentIndex].Contains(ev.Uid) ||
                EntityManager.GetComponent<MetaDataComponent>(ev.Uid).EntityLifeStage < EntityLifeStage.Initialized) return;

            _dirtyEntities[_currentIndex].Add(ev.Uid);
        }

        private void CleanupDirty(IEnumerable<IPlayerSession> sessions)
        {
            _currentIndex = (_currentIndex + 1) % DirtyBufferSize;
            _addEntities[_currentIndex].Clear();
            _dirtyEntities[_currentIndex].Clear();

            if (!CullingEnabled)
            {
                foreach (var player in sessions)
                {
                    if (player.Status != SessionStatus.InGame)
                    {
                        _oldPlayers.Remove(player);
                    }
                    else
                    {
                        _oldPlayers.Add(player);
                    }
                }
            }
        }

        private bool TryGetTick(GameTick tick, [NotNullWhen(true)] out HashSet<EntityUid>? addEntities, [NotNullWhen(true)] out HashSet<EntityUid>? dirtyEntities)
        {
            var currentTick = _gameTiming.CurTick;
            if (currentTick.Value - tick.Value >= DirtyBufferSize)
            {
                addEntities = null;
                dirtyEntities = null;
                return false;
            }

            var index = tick.Value % DirtyBufferSize;
            if (index > _dirtyEntities.Length - 1)
            {
                addEntities = null;
                dirtyEntities = null;
                return false;
            }

            addEntities = _addEntities[index];
            dirtyEntities = _dirtyEntities[index];
            return true;
        }
    }
}
