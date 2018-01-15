namespace SS14.Shared
{
    public enum ItemMessage
    {
        CreateItem = 0,
        InterpolationPacket,
        ClickItem,
        PickUpItem,
        DropItem,
        UseItem,
        Click,
        AttachTo,
        Detach
    }

    public enum UiManagerMessage
    {
        ComponentMessage,
        CreateUiElement
    }

    public enum CreateUiType
    {
        HealthScannerWindow
    }

    public enum PlacementManagerMessage
    {
        StartPlacement,
        CancelPlacement,
        PlacementFailed,
        RequestPlacement,
        RequestEntRemove,
    }

    public enum MobMessage
    {
        CreateMob = 0,
        InterpolationPacket,
        DeleteMob,
        ClickMob,
        AnimationState,
        AnimateOnce,
        SelectAppendage,
        DropItem,
        Equip,
        Unequip,
        Death
    }

    public enum MapMessage
    {
        TurfUpdate = 0,
        TurfClick,
        //TurfAddDecal,
        //TurfRemoveDecal,
        //SendTileIndex,
        SendTileMap,
        SendMapInfo,

        CreateMap,
        UnregisterMap,
    }

    public enum MobHand
    {
        RHand = 0,
        LHand
    }

    public enum GameType
    {
        MapEditor = 0,
        Game
    }

    public enum ClientStatus
    {
        Lobby = 0,
        Game
    }

    public enum PlayerSessionMessage
    {
        AttachToEntity,
        JoinLobby,
        AddPostProcessingEffect
    }

    public enum DoorState
    {
        Closed = 0,
        Open,
        Broken
    }

    public enum TileState
    {
        Healthy = 0,
        Welded,
        Wrenched,
        Dead
    }

    public enum GasType
    {
        Oxygen = 0, // MUST BE 1 FOR NETWORKING
        Toxin = 1,
        Nitrogen = 2,
        CO2 = 3,
        WVapor = 4
    }

    public enum DecalType
    {
        Blood
    }

    public enum EquipmentSlot
    {
        None = 0,
        Head,
        Mask,
        Inner,
        Outer,
        Hands,
        Feet,
        Eyes,
        Ears,
        Belt,
        Back
    }

    public enum EntityManagerMessage
    {
        SpawnEntity,
        DeleteEntity,
        InitializeEntities,
        SpawnEntityAtPosition
    }

    public enum NetworkDataType
    {
        d_enum,
        d_bool,
        d_byte,
        d_sbyte,
        d_ushort,
        d_short,
        d_int,
        d_uint,
        d_ulong,
        d_long,
        d_float,
        d_double,
        d_string,
        d_byteArray
    }
}
