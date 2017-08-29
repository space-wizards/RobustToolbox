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

    public class WorldCoordinates : Coordinates
    {
        public int MapID;

        public WorldCoordinates(Vector2 argPosition, int argMap)
        {
            Position = argPosition;
            MapID = argMap;
        }

        public WorldCoordinates(float X, float Y, int argMap)
        {
            Position = new Vector2(X, Y);
            MapID = argMap;
        }

        public LocalCoordinates ToLocal(IMapGrid grid)
        {
            return new LocalCoordinates(Position, grid);
        }
    }

    public class LocalCoordinates : Coordinates
    {
        public IMapGrid Grid;

        public LocalCoordinates(Vector2 argPosition, IMapGrid argGrid)
        {
            Position = argPosition;
            Grid = argGrid;
        }

        public LocalCoordinates(float X, float Y, IMapGrid argGrid)
        {
            Position = new Vector2(X, Y);
            Grid = argGrid;
        }

        public WorldCoordinates ToWorld()
        {
            return Grid.LocalToWorld(this);
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
