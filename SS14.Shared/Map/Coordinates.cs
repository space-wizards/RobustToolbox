using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Shared.Map
{
    [Serializable]
    public struct LocalCoordinates
    {
        public readonly int GridID;
        public readonly int MapID;
        public Vector2 Position;

        public float X
        {
            get => Position.X;
            set => new LocalCoordinates(value, Position.Y, GridID, MapID);
        }

        public float Y
        {
            get => Position.Y;
            set => new LocalCoordinates(Position.X, value, GridID, MapID);
        }

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
            if (MapID == MapManager.DEFAULTGRID)
                return this;
            var defaultgrid = IoCManager.Resolve<IMapManager>().GetMap(MapID).GetGrid(MapManager.DEFAULTGRID);
            return new LocalCoordinates(Position + Grid.WorldPosition - defaultgrid.WorldPosition, defaultgrid);
        }

        public bool InRange(LocalCoordinates localpos, float range)
        {
            if (localpos.MapID != MapID)
                return false;
            return ((localpos.ToWorld().Position - ToWorld().Position).LengthSquared < range * range);
        }

        public bool InRange(LocalCoordinates localpos, int range)
        {
            return InRange(localpos, (float)range);
        }
    }

    public struct ScreenCoordinates
    {
        public readonly int MapID;
        public Vector2 Position;

        public float X
        {
            get => Position.X;
            set => new ScreenCoordinates(value, Position.Y, MapID);
        }

        public float Y
        {
            get => Position.Y;
            set => new ScreenCoordinates(Position.X, value, MapID);
        }

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
