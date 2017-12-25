using SS14.Shared.Map;
using System.Collections.Generic;
using SS14.Shared.Interfaces.Map;
using SS14.Client.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Client.Interfaces;
using SS14.Shared.Log;

namespace SS14.Client.Map
{
    public class ClientMapManager : MapManager
    {
        [Dependency]
        private IClientTileDefinitionManager tileDefinitionManager;
        [Dependency]
        private ISceneTreeHolder sceneTree;

        private Dictionary<int, Godot.TileMap> RenderTileMaps = new Dictionary<int, Godot.TileMap>();

        public ClientMapManager()
        {
            OnTileChanged += UpdateTileMapOnUpdate;
        }

        private void UpdateTileMapOnUpdate(int gridId, TileRef tileRef, Tile oldTile)
        {
            var tilemap = RenderTileMaps[gridId];
            tilemap.SetCell(tileRef.X, tileRef.Y, tileRef.Tile.TileId);
        }

        public override IMap CreateMap(int mapID)
        {
            var ret = base.CreateMap(mapID);
            var tilemap = new Godot.TileMap();
            tilemap.SetName($"Grid #{mapID}");
            tilemap.TileSet = tileDefinitionManager.TileSet;
            tilemap.CellSize = new Godot.Vector2(32, 32);
            tilemap.Z = -10;
            sceneTree.WorldRoot.AddChild(tilemap);
            RenderTileMaps[mapID] = tilemap;

            return ret;
        }
    }
}
