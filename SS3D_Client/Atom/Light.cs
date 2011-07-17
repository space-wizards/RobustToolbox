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
        public System.Drawing.Point position;
        public System.Drawing.Point lastPosition;
        public List<Tiles.Tile> tiles;
        public Modules.Map.Map map;
        public LightDirection direction;

        public Light(Modules.Map.Map _map, System.Drawing.Color _color, int _range, LightState _state, System.Drawing.Point _position, LightDirection _direction)
        {
            map = _map;
            tiles = new List<Tiles.Tile>();
            color = _color;
            range = _range;
            state = _state;
            position = _position;
            lastPosition = _position;
            direction = _direction;
            UpdateLight();
        }

        public void UpdateLight()
        {
            foreach (Tiles.Tile t in tiles)
            {
                t.lights.Remove(this);
            }
            tiles.Clear();
            if (direction != LightDirection.All) // If we're spreading in all directions we don't want to change that if we moved
            {
                if (position.X > lastPosition.X)
                {
                    direction = LightDirection.East;
                }
                else if (position.X < lastPosition.X)
                {
                    direction = LightDirection.West;
                }
                else if (position.Y > lastPosition.Y)
                {
                    direction = LightDirection.South;
                }
                else if (position.Y < lastPosition.Y)
                {
                    direction = LightDirection.North;
                }
            }

            map.compute_visibility(position.X, position.Y, this);
        }

    }
}
