using Lidgren.Network;

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
    JobList,
    RequestJob,
    JobSelected,
    SendMap,
    MapMessage,
    ItemMessage, // It's something the item system needs to handle
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
    RequestEntityDeletion  //Client asks to delete entity. Used for editing. Requires admin.
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
    ComponentMessage
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
    TurfRemoveDecal
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
    AttachToAtom,
    Verb,
    JoinLobby,
    SetTargetArea
}

public enum DoorState
{
    Closed = 0,
    Open,
    Broken
}

public enum LightState
{
    Off = 0,
    On
}

public enum Direction
{
    East = 0,
    South,
    West,
    North,
    All
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
    Oxygen = 1, // MUST BE 1 FOR NETWORKING
    Toxin = 2,
    Nitrogen = 3,
    CO2 = 4,
    WVapor = 5,
    HighVel = 15
}

public enum DecalType
{
    Blood
}

public enum GUIBodyPart
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
}

public enum EntityManagerMessage
{
    SpawnEntity,
    DeleteEntity,
    InitializeEntities
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