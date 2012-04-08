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
        public Vector2D Position { get; private set; }
        public int Radius { get; private set; }

        public void Move(Vector2D toPosition)
        {
            Position = toPosition;
        }

        public void SetRadius(int radius)
        {
            Radius = radius;
        }

    }
}
