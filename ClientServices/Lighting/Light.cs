using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ClientInterfaces;
using ClientInterfaces.Lighting;
using ClientInterfaces.Map;
using ClientServices.Map.Tiles;
using GorgonLibrary;
using SS13_Shared;

namespace ClientServices.Lighting
{
    public class Light : ILight
    {
        private readonly IMapManager _mapManager;
        private readonly IList<Tile> _tiles = new List<Tile>();

        public Color Color = Color.PapayaWhip;
        public float Brightness { get; set; }

        public LightState State;

        #region ILight members

        public Vector2D Position { get; private set; }
        public int Range { get; set; }
        public void ClearTiles()
        {
            foreach (var t in _tiles)
            {
                t.tileLights.Remove(this);
            }
            _tiles.Clear(); 
        }
        public List<object> GetTiles()
        { return _tiles.ToList<object>(); }
        public void AddTile(object tile)
        { _tiles.Add((Tile)tile); }

        #endregion

        public Light(IMapManager map, Color color, int range, LightState state, Vector2D position)
        {
            _mapManager = map;

            Position = position;
            Color = color;
            Range = range;
            State = state;
            Brightness = 1.10f;

            UpdateLight();
        }

        public void UpdateLight()
        {
            _mapManager.LightComputeVisibility(Position, this);
        }

        public void UpdatePosition(Vector2D newPosition)
        {
            if (Position != newPosition)
            {
                Position = newPosition;
                UpdateLight();
            }
        }

    }
}
