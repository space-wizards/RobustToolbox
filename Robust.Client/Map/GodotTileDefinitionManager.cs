using Robust.Client.ResourceManagement;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using System.Collections.Generic;
using Robust.Client.Interfaces.Map;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement.ResourceTypes;
using Robust.Shared.Utility;

namespace Robust.Client.Map
{
    /// <summary>
    ///     Special TileDefinitionManager that makes a Godot TileSet for usage by TileMaps.
    /// </summary>
    internal sealed class GodotTileDefinitionManager : TileDefinitionManager, IGodotTileDefinitionManager
    {
        [Dependency] readonly IResourceCache resourceCache;

        public Godot.TileSet TileSet { get; private set; }

        private Dictionary<ushort, TextureResource> Textures = new Dictionary<ushort, TextureResource>();

        public GodotTileDefinitionManager()
        {
            TileSet = new Godot.TileSet();
        }

        public override void Register(ITileDefinition tileDef)
        {
            base.Register(tileDef);

            var id = tileDef.TileId;
            TileSet.CreateTile(id);
            if (string.IsNullOrEmpty(tileDef.SpriteName))
            {
                return;
            }

            var texture =
                resourceCache.GetResource<TextureResource>(
                    new ResourcePath("/Textures/Tiles/") / $@"{tileDef.SpriteName}.png");
            TileSet.TileSetTexture(id, texture.Texture.GodotTexture);
            Textures[id] = texture;
        }
    }
}
