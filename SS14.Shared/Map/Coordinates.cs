using OpenTK;
using SS14.Shared.Interfaces.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Shared.Map
{
    public abstract class Coordinates
    {
        public const int NULLSPACE = 0;
        public Vector2 Position;

        public float X
        {
            get => Position.X;
            set => Position = new Vector2(value, Position.Y);
        }

        public float Y
        {
            get => Position.Y;
            set => Position = new Vector2(Position.X, value);
        }
    }

    public class LocalCoordinates : Coordinates
    {
        public IMapGrid Grid;
        public int MapID;

        public LocalCoordinates(Vector2 argPosition, IMapGrid argGrid)
        {
            Position = argPosition;
            Grid = argGrid;
            MapID = argGrid.MapID;
        }

        public LocalCoordinates(float X, float Y, IMapGrid argGrid)
        {
            Position = new Vector2(X, Y);
            Grid = argGrid;
            MapID = argGrid.MapID;
        }

        public LocalCoordinates(float X, float Y, int argGrid, int argMap)
        {
            Position = new Vector2(X, Y);
            MapID = argMap;
            argGrid = GetMap(argMap).GetGrid(argGrid);
        }
    }

    public class ScreenCoordinates : Coordinates
    {
        public int MapID;

        public ScreenCoordinates(Vector2 argPosition, int argMap)
        {
            Position = argPosition;
            MapID = argMap;
        }

        public ScreenCoordinates(float X, float Y, int argMap)
        {
            Position = new Vector2(X, Y);
            MapID = argMap;
        }
    }
}
