using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using System;

namespace SS14.Shared.Map
{
    [Serializable]
    public struct LocalCoordinates
    {
        public readonly GridId GridID;
        public readonly MapId MapID;
        public readonly Vector2 Position;

        public float X => Position.X;

        public float Y => Position.Y;

        public IMap Map => IoCManager.Resolve<IMapManager>().GetMap(MapID);

        public IMapGrid Grid => IoCManager.Resolve<IMapManager>().GetMap(MapID).GetGrid(GridID);


        public LocalCoordinates(Vector2 argPosition, IMapGrid argGrid)
        {
            Position = argPosition;
            GridID = argGrid.Index;
            MapID = argGrid.MapID;
        }

        public LocalCoordinates(Vector2 argPosition, GridId argGrid, MapId argMap)
        {
            Position = argPosition;
            GridID = argGrid;
            MapID = argMap;
        }

        public LocalCoordinates(float X, float Y, IMapGrid argGrid)
        {
            Position = new Vector2(X, Y);
            GridID = argGrid.Index;
            MapID = argGrid.MapID;
        }

        public LocalCoordinates(float X, float Y, GridId argGrid, MapId argMap)
        {
            Position = new Vector2(X, Y);
            GridID = argGrid;
            MapID = argMap;
        }

        public LocalCoordinates ConvertToGrid(IMapGrid argGrid)
        {
            return new LocalCoordinates(Position + Grid.WorldPosition - argGrid.WorldPosition, argGrid);
        }

        public LocalCoordinates ToWorld()
        {
            if (MapID == MapId.Nullspace)
                return this;

            var defaultgrid = IoCManager.Resolve<IMapManager>().GetMap(MapID).GetGrid(GridId.DefaultGrid);
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
        public readonly MapId MapID;
        public readonly Vector2 Position;

        public float X => Position.X;

        public float Y => Position.Y;

        public ScreenCoordinates(Vector2 argPosition, MapId argMap)
        {
            Position = argPosition;
            MapID = argMap;
        }

        public ScreenCoordinates(float X, float Y, MapId argMap)
        {
            Position = new Vector2(X, Y);
            MapID = argMap;
        }
    }
}
