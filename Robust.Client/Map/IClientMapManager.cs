using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Robust.Client.Map
{
    internal interface IClientMapManager : IMapManagerInternal
    {
        // Two methods here, so that new grids etc can be made BEFORE entities get states applied,
        // but old ones can be deleted after.
        void ApplyGameStatePre(GameStateMapData? data);
        void ApplyGameStatePost(GameStateMapData? data);
    }
}
