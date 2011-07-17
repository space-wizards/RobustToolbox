using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GorgonLibrary;
using GorgonLibrary.Graphics;

namespace SS3D.Atom
{
    public class Light
    {
        public System.Drawing.Color color;
        public int range;
        public LightState state;
        public System.Drawing.Point tilePosition;
        public Vector2D position;
        public Vector2D lastPosition;
        public List<Tiles.Tile> tiles;
        public Modules.Map.Map map;
        public List<LightDirection> direction;

        public Light(Modules.Map.Map _map, System.Drawing.Color _color, int _range, LightState _state, System.Drawing.Point _position, LightDirection _direction)
        {
            map = _map;
            tiles = new List<Tiles.Tile>();
            color = _color;
            range = _range;
            state = _state;
            tilePosition = _position;
            lastPosition = _position;
            direction = new List<LightDirection>();
            direction.Add(_direction);
            UpdateLight();
        }

        public void UpdateLight()
        {
            LightDirection lastDir = direction[0];
            foreach (Tiles.Tile t in tiles)
            {
                t.lights.Remove(this);
            }
            tiles.Clear();
            if (!direction.Contains(LightDirection.All)) // If we're spreading in all directions we don't want to change that if we moved
            {
                direction.Clear();
                if (position.X > lastPosition.X)
                {
                    direction.Add(LightDirection.East);
                }
                else if (position.X < lastPosition.X)
                {
                    direction.Add(LightDirection.West);
                }
                if (position.Y > lastPosition.Y)
                {
                    direction.Add(LightDirection.South);
                }
                else if (position.Y < lastPosition.Y)
                {
                    direction.Add(LightDirection.North);
                }
            }
            if (direction.Count == 0)
                direction.Add(lastDir);
            map.compute_visibility(tilePosition.X, tilePosition.Y, this);
        }

        public void UpdatePosition(Vector2D newPosition)
        {
            lastPosition = position;
            position = newPosition;
            tilePosition = map.GetTileArrayPositionFromWorldPosition(position);
            if (position != lastPosition)
            {
                UpdateLight();
            }

        }

    }
}
