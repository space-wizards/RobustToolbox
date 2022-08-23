namespace Robust.Shared.Enums
{
    public enum PlacementManagerMessage : byte
    {
        StartPlacement,
        CancelPlacement,
        PlacementFailed,
        RequestPlacement,
        RequestEntRemove,
        RequestRectRemove,
    }

    public enum SessionStatus : byte
    {
        Zombie = 0,
        Connecting,
        Connected,
        InGame,
        Disconnected
    }
}
