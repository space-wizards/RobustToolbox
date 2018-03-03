namespace SS14.Shared.Input
{
    public enum BoundKeyState
    {
        Up,
        Down,
        Repeat
    }

    /// <summary>
    /// Key Bindings - each corresponds to a logical function ingame.
    /// </summary>
    public enum BoundKeyFunctions
    {
        MoveUp,
        MoveDown,
        MoveLeft,
        MoveRight,
        SwitchHands,
        Inventory,
        ShowFPS,
        Drop,
        Run,
        ActivateItemInHand,
    }
}
