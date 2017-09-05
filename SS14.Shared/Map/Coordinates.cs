using OpenTK;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
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
        public int GridID;
        public int MapID;

        public IMap Map => IoCManager.Resolve<IMapManager>().GetMap(MapID);

        public IMapGrid Grid => IoCManager.Resolve<IMapManager>().GetMap(MapID).GetGrid(GridID);


        public LocalCoordinates(Vector2 argPosition, IMapGrid argGrid)
        {
            Position = argPosition;
            GridID = argGrid.Index;
            MapID = argGrid.MapID;
        }

        public LocalCoordinates(Vector2 argPosition, int argGrid, int argMap)
        {
            Position = argPosition;
            GridID = argGrid;
            MapID = argGrid;
        }

        public LocalCoordinates(float X, float Y, IMapGrid argGrid)
        {
            Position = new Vector2(X, Y);
            GridID = argGrid.Index;
            MapID = argGrid.MapID;
        }

        public LocalCoordinates(float X, float Y, int argGrid, int argMap)
        {
            Position = new Vector2(X, Y);
            MapID = argMap;
            GridID = argGrid;
        }

        public LocalCoordinates ConvertToGrid(IMapGrid argGrid)
        {
            return new LocalCoordinates(Position + Grid.WorldPosition - argGrid.WorldPosition, argGrid);
        }

        public LocalCoordinates ToWorld()
        {
            var defaultgrid = IoCManager.Resolve<IMapManager>().GetMap(MapID).GetGrid(0);
            return new LocalCoordinates(Position + Grid.WorldPosition - defaultgrid.WorldPosition, defaultgrid);
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
