using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClientInterfaces;
using GorgonLibrary;
using ClientMap;

namespace ClientLighting
{
    public class Light : ILight
    {
        public System.Drawing.Color color = System.Drawing.Color.PapayaWhip;
        public int range = 150;
        public float brightness = 1.10f;

        public LightState state;
        public Vector2D position;
        public Vector2D lastPosition;
        public Map map;
        public List<ClientMap.Tiles.Tile> tiles;

        #region ILight members
        public Vector2D Position { get { return position; } set { position = value; } }
        public int Range { get { return range; } set { range = value; } }
        public void ClearTiles()
        { tiles.Clear(); }
        public List<object> GetTiles()
        { return tiles.ToList<object>(); }
        public void AddTile(object tile)
        { tiles.Add((ClientMap.Tiles.Tile)tile); }

        #endregion

        public Light(Map _map, System.Drawing.Color _color, int _range, LightState _state, Vector2D _position)
        {
            Random r = new Random();
            map = _map;
            color = _color;
            range = _range;
            state = _state;
            lastPosition = _position;
            tiles = new List<ClientMap.Tiles.Tile>();
            UpdateLight();
        }

        public void UpdateLight()
        {
            map.light_compute_visibility(position, this);
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
