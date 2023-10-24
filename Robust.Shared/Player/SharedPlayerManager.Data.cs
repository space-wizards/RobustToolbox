using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Network;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Player;

// This partial class contains code related to player data.
internal abstract partial class SharedPlayerManager
{
    [ViewVariables]
    protected readonly Dictionary<NetUserId, SessionData> PlayerData = new();

    public SessionData GetPlayerData(NetUserId userId)
    {
        return PlayerData[userId];
    }

    public bool TryGetPlayerData(NetUserId userId, [NotNullWhen(true)] out SessionData? data)
    {
        return PlayerData.TryGetValue(userId, out data);
    }

    public bool TryGetPlayerDataByUsername(string userName, [NotNullWhen(true)] out SessionData? data)
    {
        data = null;
        return UserIdMap.TryGetValue(userName, out var userId) && PlayerData.TryGetValue(userId, out data);
    }

    public bool HasPlayerData(NetUserId userId)
    {
        return PlayerData.ContainsKey(userId);
    }

    public IEnumerable<SessionData> GetAllPlayerData()
    {
        return PlayerData.Values;
    }
}
