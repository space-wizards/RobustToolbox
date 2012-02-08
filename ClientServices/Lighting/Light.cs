using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ClientInterfaces;
using ClientServices.Map.Tiles;
using GorgonLibrary;
using SS13_Shared;

namespace ClientServices.Lighting
{
    public class Light : ILight
    {
        public Color color = Color.PapayaWhip;
        public int range = 150;
        public float Brightness { get; set; }

        public LightState state;
        public Vector2D position;
        public Vector2D lastPosition;
        private readonly IMapManager _mapManager;
        public List<Tile> tiles;

        #region ILight members
        public Vector2D Position { get { return position; } set { position = value; } }
        public int Range { get { return range; } set { range = value; } }
        public void ClearTiles()
        {
            foreach (ClientServices.Map.Tiles.Tile t in tiles)
            {
                t.tileLights.Remove(this);
            }
            tiles.Clear(); 
        }
        public List<object> GetTiles()
        { return tiles.ToList<object>(); }
        public void AddTile(object tile)
        { tiles.Add((ClientServices.Map.Tiles.Tile)tile); }

        #endregion

        public Light(IMapManager map, Color _color, int _range, LightState _state, Vector2D _position)
        {
            _mapManager = map;
            color = _color;
            range = _range;
            state = _state;
            lastPosition = _position;
            tiles = new List<Tile>();
            Brightness = 1.10f;
            UpdateLight();
        }

        public void UpdateLight()
        {
            _mapManager.LightComputeVisibility(position, this);
        }

        public void UpdatePosition(Vector2D newPosition)
        {
            lastPosition = position;
            position = newPosition;
            if (position != lastPosition)
            {
                UpdateLight();
            }
        }

    }
}
