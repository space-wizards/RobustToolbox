using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Network;

namespace Robust.Shared.Placement;

public readonly struct PlacementEntityEvent
{
    public readonly EntityUid EditedEntity;

    public readonly EntityCoordinates Coordinates;

    public readonly PlacementEventAction PlacementEventAction;

    public readonly NetUserId? PlacerNetUserId;

    public PlacementEntityEvent(EntityUid editedEntity, EntityCoordinates coordinates, PlacementEventAction placementEventAction, NetUserId? placerNetUserId)
    {
        EditedEntity = editedEntity;
        Coordinates = coordinates;
        PlacementEventAction = placementEventAction;
        PlacerNetUserId = placerNetUserId;
    }
}

public readonly struct PlacementTileEvent
{
    public readonly int TileType;

    public readonly EntityCoordinates Coordinates;

    public readonly NetUserId? PlacerNetUserId;

    public PlacementTileEvent(int tileType, EntityCoordinates coordinates, NetUserId? placerNetUserId)
    {
        TileType = tileType;
        Coordinates = coordinates;
        PlacerNetUserId = placerNetUserId;
    }
}

public enum PlacementEventAction
{
    Erase,
    Create,
}
