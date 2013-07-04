using System;

namespace SS13_Shared
{
    public enum NetMessage
    {
        GameType = 0,
        LobbyChat,
        ServerName,
        ClientName,
        WelcomeMessage,
        MaxPlayers,
        PlayerCount,
        PlayerList,
        RequestMap,
        JobList,
        RequestJob,
        JobSelected,
        MapMessage,
        ItemMessage, // It's something the item system needs to handle
        CraftMessage,
        MobMessage,
        ChatMessage,
        PlacementManagerMessage,
        PlayerSessionMessage,
        PlayerUiMessage,
        JoinGame,
        ForceRestart,
        AtmosDisplayUpdate,
        EntityMessage,
        EntityManagerMessage,
        RequestAdminLogin,      //Server asks client to login OR client tries to login.
        RequestAdminPlayerlist, //Client request playerlist for admin panel OR server sends it.
        RequestAdminBan,
        RequestAdminKick,
        RequestBanList,
        RequestAdminUnBan,
        RequestEntityDeletion,  //Client asks to delete entity. Used for editing. Requires admin.
        StateUpdate,
        StateAck,
        FullState
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

    public enum CraftMessage
    {
        StartCraft
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
        TurfAddDecal,
        TurfRemoveDecal,
        SendTileIndex,
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
        SetTargetArea,
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
        WVapor = 4,
        HighVel = 5
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
        d_string
    }
}