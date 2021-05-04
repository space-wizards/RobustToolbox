using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Robust.Server.Map
{
    internal interface IServerMapManager : IMapManagerInternal
    {
        GameStateMapData? GetStateData(GameTick fromTick);
        void CullDeletionHistory(GameTick uptoTick);
    }
}
