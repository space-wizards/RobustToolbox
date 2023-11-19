using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.GameStates;

internal sealed partial class PvsSystem
{
    /// <summary>
    /// History of deletion-tuples, containing the <see cref="GameTick"/> of the deletion, as well as the <see cref="TIndex"/> of the object which was deleted.
    /// </summary>
    private readonly List<(GameTick tick, NetEntity ent)> _deletionHistory = new();

    /// <inheritdoc />
    public void CullDeletionHistoryUntil(GameTick tick)
    {
        if (tick == GameTick.MaxValue)
        {
            _deletionHistory.Clear();
            return;
        }

        for (var i = _deletionHistory.Count - 1; i >= 0; i--)
        {
            var hist = _deletionHistory[i].tick;
            if (hist <= tick)
            {
                _deletionHistory.RemoveSwap(i);
                if (_largestCulled < hist)
                    _largestCulled = hist;
            }
        }
    }

    private GameTick _largestCulled;

    public void GetDeletedEntities(GameTick fromTick, IList<NetEntity> ents)
    {
        if (fromTick == GameTick.Zero)
            return;

        // I'm 99% sure this can never happen, but it is hard to test real laggy/lossy networks with many players.
        if (_largestCulled > fromTick)
        {
            Log.Error($"Culled required deletion history! culled: {_largestCulled}. requested: > {fromTick}");
            _largestCulled = GameTick.Zero;
        }

        foreach (var (tick, id) in _deletionHistory)
        {
            if (tick > fromTick)
                ents.Add(id);
        }
    }
}
