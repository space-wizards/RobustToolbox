namespace SS14.Shared
{
    public enum NetMessages
    {
        // Base engine messages
        ERRROR = 0,
        ClientName,             // C>S CL_GREET, This contains all info for server to create a CL_INFO
        WelcomeMessageReq,      // C>S D Requests a welcome message, the server should send this to all established connections anyways.
        WelcomeMessage,         // S<? D SERVER_INFO
        PlayerListReq,          // C>S
        PlayerList,             // S>C D CL_INFO, A list of CL_Info's

        // Console Commands
        LobbyChat,              //
        ChatMessage,            // 
        PlayerSessionMessage,   //
        ConsoleCommand,         //
        ConsoleCommandReply,    //
        ConsoleCommandRegister, //

        // Map Messages
        RequestMap,
        MapMessage,

        PlacementManagerMessage,
        PlayerUiMessage,
        JoinGame,               // C>S Asks the server to move from lobby to the game. 
        ForceRestart,
        EntityMessage,
        RequestEntityDeletion, //Client asks to delete entity. Used for editing. Requires admin.
        StateUpdate,
        StateAck,
        FullState,
    }

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
        RequestPlacement
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
        SendTileMap
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
        Verb,
        JoinLobby,
        AddPostProcessingEffect
    }

    public enum DoorState
    {
        Closed = 0,
        Open,
        Broken
    }

    public enum Direction
    {
        North = 0,
        NorthEast,
        East,
        SouthEast,
        South,
        SouthWest,
        West,
        NorthWest
    }

    [System.Flags]
    public enum DirectionFlags
    {
        None = 0,
        North = 1,
        East = 2,
        South = 4,
        West = 8
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

    public enum EntityMessage
    {
        ComponentMessage,
        SystemMessage,
        PositionMessage,
        ComponentInstantiationMessage,
        Null,
        SetDirection,
        NameUpdate,
        SetSVar,
        GetSVars,
        SetCVar,
        GetCVars
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
