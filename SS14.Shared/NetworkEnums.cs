namespace SS14.Shared
{
    //TODO: This will be removed once the Client gets migrated to the new network system.
    /// <summary>
    /// Contains all NetMessage IDs.
    /// </summary>
    public enum NetMessages
    {
        // Base engine messages
        Error = 0,
        ClientName,             // C>S Sends the server its client info.
        WelcomeMessageReq,      // C>S Requests the server info.
        WelcomeMessage,         // S>C Server info.
        PlayerListReq,          // C>S Requests a full list of players.
        PlayerList,             // S>C A full list of players.
        StringTableEntry,       // S>C An entry into the string table.

        // Console Commands
        ChatMessage,            // C<>S Contains all of the chat messages.
        PlayerSessionMessage,   // C>S Tells (lol.) the server about state changes.
        ConsoleCommand,         // C>S Sends the server a console command.
        ConsoleCommandReply,    // S>C Acknowledges a received console command.
        ConsoleCommandRegister, // S>C Registers all console commands.

        // Map Messages
        RequestMap,             // C>S Requests a full copy of the map.
        MapMessage,             // S>C Sends a full copy of the map.

        // misc stuff that will prob be removed
        PlacementManagerMessage,// S<>C Contains all placement messages.
        PlayerUiMessage,        // S>C Sends a user interface message.

        // entity stuff
        EntityMessage,          // S<>C Contains all entity messages.
        RequestEntityDeletion,  // C>S Client asks to delete entity.
        StateUpdate,            // S>C Delta state update.
        StateAck,               // C>S Acknowledges a state update.
        FullState,              // S>C Full state of the game.
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
        SendTileMap,
        SendMapInfo
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

    public enum EntityMessage
    {
        ComponentMessage,
        SystemMessage,
        PositionMessage,
        Null,
        SetDirection,
        NameUpdate,
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
