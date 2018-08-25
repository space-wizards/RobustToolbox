using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Prototypes;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace SS14.Shared.Map
{
    // Instantiated by the Prototype system through reflection.
    [Prototype("tile")]
    public sealed class PrototypeTileDefinition : TileDefinition, IPrototype
    {
        internal ushort FutureID { get; private set; }

        public void LoadFrom(YamlMappingNode mapping)
        {
            Name = mapping.GetNode("name").ToString();
            SpriteName = mapping.GetNode("texture").ToString();
            FutureID = (ushort)mapping.GetNode("id").AsInt();
        }
    }
}
