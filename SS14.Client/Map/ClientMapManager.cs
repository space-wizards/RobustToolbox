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

        private Dictionary<(MapId mapId, GridId gridId), Godot.TileMap> RenderTileMaps = new Dictionary<(MapId mapId, GridId gridId), Godot.TileMap>();

        public ClientMapManager()
        {
            TileChanged += UpdateTileMapOnUpdate;
            OnGridCreated += UpdateOnGridCreated;
            OnGridRemoved += UpdateOnGridRemoved;
            GridChanged += UpdateOnGridModified;
        }

        private void UpdateOnGridModified(object sender, GridChangedEventArgs args)
        {
            var tilemap = RenderTileMaps[(args.Grid.MapID, args.Grid.Index)];
            foreach ((int x, int y, Tile tile) in args.Modified)
            {
                tilemap.SetCell(x, y, tile.TileId);
            }
        }

        private void UpdateTileMapOnUpdate(object sender, TileChangedEventArgs args)
        {
            var tilemap = RenderTileMaps[(args.NewTile.MapIndex, args.NewTile.GridIndex)];
            tilemap.SetCell(args.NewTile.X, args.NewTile.Y, args.NewTile.Tile.TileId);
        }

        private void UpdateOnGridCreated(MapId mapId, GridId gridId)
        {
            var tilemap = new Godot.TileMap
            {
                TileSet = tileDefinitionManager.TileSet,
                // TODO: Unhardcode this cell size.
                CellSize = new Godot.Vector2(32, 32),
                ZIndex = -10,
                // Fiddle with this some more maybe. Increases lighting performance a TON.
                CellQuadrantSize = 4,
                //Visible = false,
            };
            tilemap.SetName($"Grid {mapId}.{gridId}");
            sceneTree.WorldRoot.AddChild(tilemap);
            RenderTileMaps[(mapId, gridId)] = tilemap;
        }

        private void UpdateOnGridRemoved(MapId mapId, GridId gridId)
        {
            Logger.Debug($"Removing grid {mapId}.{gridId}");
            var tilemap = RenderTileMaps[(mapId, gridId)];
            tilemap.QueueFree();
            tilemap.Dispose();
            RenderTileMaps.Remove((mapId, gridId));
        }
    }
}
