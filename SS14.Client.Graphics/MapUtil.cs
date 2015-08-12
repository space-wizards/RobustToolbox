using SS14.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Client.Graphics
{
    public abstract class MapUtil
    {
        public static readonly int TileSize = 32;

        public static Vector2 worldToTileSize(Vector2 worldVect)
        {
            return worldVect / TileSize;
        }

        public static RectangleF worldToTileSize(RectangleF rect)
        {
            return new RectangleF((Vector2)rect.Location / TileSize, (Vector2)rect.Size / TileSize);
        }

        public static PointF worldToTileSize(PointF point)
        {
            return (Vector2)point / TileSize;
        }

        public static Vector2 tileToWorldSize(Vector2 tileVect)
        {
            return tileVect * TileSize;
        }

        public static RectangleF tileToWorldSize(RectangleF rect)
        {
            return new RectangleF((Vector2)rect.Location * TileSize, (Vector2)rect.Size * TileSize);
        }

        public static PointF tileToWorldSize(PointF point)
        {
            return (Vector2)point * TileSize;
        }
    }
}
