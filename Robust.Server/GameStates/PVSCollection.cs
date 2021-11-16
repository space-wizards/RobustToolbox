using System;
using System.Collections.Generic;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Timing;

namespace Robust.Server.GameStates;

public class PVSCollection<TIndex> where TIndex : IComparable<TIndex>, IEquatable<TIndex>
{
    private readonly Dictionary<Vector2i, TIndex> _mapChunkContents = new();
    private readonly Dictionary<GridId, Dictionary<Vector2i, TIndex>> _gridChunkContents = new();

    private readonly HashSet<TIndex> _globalOverrides = new();
    private readonly Dictionary<ICommonSession, HashSet<TIndex>> _localOverrides = new();

    private readonly List<(GameTick tick, TIndex index)> _deletionHistory = new();

    #region Init Functions

    public void AddPlayer(ICommonSession session) => _localOverrides[session] = new();
    public void RemovePlayer(ICommonSession session) => _localOverrides.Remove(session);

    public void AddGrid(GridId gridId) => _gridChunkContents[gridId] = new();
    public void RemoveGrid(GridId gridId) => _gridChunkContents.Remove(gridId);

    #endregion

    public void RemoveIndex(GameTick tick, TIndex index) => _deletionHistory.Add((tick, index));
    public void CullDeletionHistoryUntil(GameTick tick) => _deletionHistory.RemoveAll(hist => hist.tick < tick);

}
