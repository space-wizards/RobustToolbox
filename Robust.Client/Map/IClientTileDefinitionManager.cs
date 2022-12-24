using Robust.Client.Graphics;
using Robust.Shared.Map;

namespace Robust.Client.Map;

public interface IClientTileDefinitionManager : ITileDefinitionManager
{
    /// <summary>
    /// Returns the relevant texture for this tile and its specified variant.
    /// </summary>
    Texture GetTexture(Tile tile);
}
