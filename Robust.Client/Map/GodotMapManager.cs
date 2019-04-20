using Robust.Shared.Map;
using System.Collections.Generic;
using Robust.Shared.Interfaces.Map;
using Robust.Client.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Client.Interfaces;
using Robust.Shared.Log;
using Robust.Client.Graphics.ClientEye;

namespace Robust.Client.Map
{
    public class GodotMapManager : MapManager
    {
        [Dependency]
        private IGodotTileDefinitionManager tileDefinitionManager;
        [Dependency]
        private ISceneTreeHolder sceneTree;

        private Dictionary<GridId, Godot.TileMap> RenderTileMaps = new Dictionary<GridId, Godot.TileMap>();

        public GodotMapManager()
        {
            if (!GameController.OnGodot)
            {
                return;
            }
            TileChanged += UpdateTileMapOnUpdate;
            OnGridCreated += UpdateOnGridCreated;
            OnGridRemoved += UpdateOnGridRemoved;
            GridChanged += UpdateOnGridModified;
        }

        private void UpdateOnGridModified(object sender, GridChangedEventArgs args)
        {
            var tilemap = RenderTileMaps[args.Grid.Index];
            foreach (var (index, tile) in args.Modified)
            {
                tilemap.SetCell(index.X, -1-index.Y, tile.TypeId);
            }
        }

        private void UpdateTileMapOnUpdate(object sender, TileChangedEventArgs args)
        {
            var tilemap = RenderTileMaps[args.NewTile.GridIndex];
            tilemap.SetCell(args.NewTile.X, -1-args.NewTile.Y, args.NewTile.Tile.TypeId);
        }

        private void UpdateOnGridCreated(GridId gridId)
        {
            var tilemap = new Godot.TileMap
            {
                TileSet = tileDefinitionManager.TileSet,
                // TODO: Unhardcode this cell size.
                CellSize = new Godot.Vector2(EyeManager.PIXELSPERMETER, EyeManager.PIXELSPERMETER),
                ZIndex = -10,
                // Fiddle with this some more maybe. Increases lighting performance a TON.
                CellQuadrantSize = 4,
                //Visible = false,
            };
            tilemap.SetName($"Grid {gridId}");
            sceneTree.WorldRoot.AddChild(tilemap);
            // Creating a map makes a grid before mapcreated is fired, so...
            RenderTileMaps[gridId] = tilemap;
        }

        private void UpdateOnGridRemoved(GridId gridId)
        {
            Logger.Debug($"Removing grid {gridId}");
            var tilemap = RenderTileMaps[gridId];
            tilemap.QueueFree();
            tilemap.Dispose();
            RenderTileMaps.Remove(gridId);
        }
    }
}
