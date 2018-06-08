using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Prototypes;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace SS14.Shared.Map
{
    /// <summary>
    ///     Default TileDefinition of a Floor
    /// </summary>
    public sealed class FloorTileDefinition : TileDefinition
    {
        /// <summary>
        ///     Default Constructor
        /// </summary>
        public FloorTileDefinition()
        {
            Name = "Floor";

            IsConnectingSprite = false;
            IsOpaque = false;
            IsCollidable = false;
            IsGasVolume = true;
            IsVentedIntoSpace = false;
            SpriteName = "floor_texture";
        }
    }

    /// <summary>
    ///     Default TileDefinition of Space
    /// </summary>
    public sealed class SpaceTileDefinition : TileDefinition
    {
        /// <summary>
        ///     Default Constructor
        /// </summary>
        public SpaceTileDefinition()
        {
            Name = "Space";

            IsConnectingSprite = false;
            IsOpaque = false;
            IsCollidable = false;
            IsGasVolume = true;
            IsVentedIntoSpace = true;
        }
    }

    // Instantiated by the Prototype system through reflection.
    [Prototype("tile")]
    public sealed class PrototypeTileDefinition : TileDefinition, IPrototype
    {
        public void LoadFrom(YamlMappingNode mapping)
        {
            Name = mapping.GetNode("name").ToString();
            SpriteName = mapping.GetNode("texture").ToString();
            
            // register us with the tile system
            Register(IoCManager.Resolve<ITileDefinitionManager>());
        }
    }
}
