namespace SS14.Shared.GameObjects
{
    public enum ComponentMessageType
    {
        Null,
        Empty,
        BoundKeyChange,
        BoundKeyRepeat,
        SlaveAttach,
        LeftClick,
        RightClick,
        AltLeftClick,
        AltRightClick,
        ShiftLeftClick,
        ShiftRightClick,
        CtrlLeftClick,
        CtrlRightClick,
        SetSpriteByKey,
        SetVisible,
        Dropped,
        PickedUp,
        MoveDirection,
        SpriteChanged,
        GetSprite,
        CurrentSprite,
        ReceiveEmptyHandToLargeObjectInteraction,
        ItemUnEquipped,
        ItemEquipped,
        Die,
        Bumped,
        Initialize,
        ReturnActorConnection,
        ReturnActorSession,
        GetActorConnection,
        GetActorSession,
        EntitySaidSomething,
        ContextAdd,
        ContextRemove,
        ContextGetEntries,
        ContextMessage, //Sent when context menu option is clicked.
        GetDescriptionString, //Sent when description is requested and when answer is sent.
        Live,
        ClickedInHand,
        Activate,
        SetBaseName,
        PhysicsMove,
    }

    public enum DrawDepth
    {
        FloorTiles = 0,
        FloorObjects = 1,
        ItemsOnFloor = 2,
        MobBase = 3,
        MobUnderClothingLayer = 4,
        MobUnderAccessoryLayer = 5,
        MobOverClothingLayer = 6,
        MobOverAccessoryLayer = 7,
        HeldItems = 8,
        Tables = 9,
        ItemsOnTables = 10,
        FloorPlaceable = 11,
        Doors = 12,
        Walls = 13,
        WallMountedItems = 14,
        WallTops = 15,
        LightOverlay = 16
    }
}
