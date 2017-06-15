using SS14.Server.Interfaces.Map;
using SS14.Shared.IoC;
using System.Diagnostics;

namespace SS14.Server.Map
{
    [System.Diagnostics.DebuggerDisplay("TileDef: {Name}")]
    public class TileDefinition : ITileDefinition
    {
        ushort tileId = ushort.MaxValue;
        public ushort TileId
        {
            get {
                if (tileId == ushort.MaxValue)
                    tileId = IoCManager.Resolve<ITileDefinitionManager>().Register(this);

                Debug.Assert(tileId != ushort.MaxValue);
                return tileId;
            }
        }

        public string Name { get; protected set; }

        public bool IsConnectingSprite { get; protected set; }
        public bool IsOpaque { get; protected set; }
        public bool IsCollidable { get; protected set; }
        public bool IsGasVolume { get; protected set; }
        public bool IsVentedIntoSpace { get; protected set; }
        public bool IsWall { get; protected set; }
    }
}
