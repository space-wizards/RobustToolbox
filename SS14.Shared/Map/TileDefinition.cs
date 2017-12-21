using System.Diagnostics;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;

namespace SS14.Shared.Map
{
    [DebuggerDisplay("TileDef: {Name}")]
    public abstract class TileDefinition : ITileDefinition
    {
        private ushort _tileId = ushort.MaxValue;

        public ushort TileId
        {
            get
            {
                Debug.Assert(_tileId != ushort.MaxValue);
                return _tileId;
            }
        }

        public void InvalidateTileId()
        {
            _tileId = ushort.MaxValue;
        }

        public void Register(ITileDefinitionManager tileDefinitionManager)
        {
            _tileId = tileDefinitionManager.Register(this);
        }

        public string Name { get; protected set; }

        public bool IsConnectingSprite { get; protected set; }

        public bool IsOpaque { get; protected set; }

        public bool IsCollidable { get; protected set; }

        public bool IsGasVolume { get; protected set; }

        public bool IsVentedIntoSpace { get; protected set; }

        public bool IsFloor { get; protected set; }

        public string SpriteName { get; protected set; }

        /// <summary>
        /// Creates a new tile instance from this definition.
        /// </summary>
        /// <param name="data">Optional per-tile data.</param>
        /// <returns></returns>
        public Tile Create(ushort data = 0) { return new Tile(TileId, data); }
    }
}
