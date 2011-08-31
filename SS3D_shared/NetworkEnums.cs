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
    SendMap,
    MapMessage,
    ItemMessage, // It's something the item system needs to handle
    MobMessage,
    ChatMessage,
    AtomManagerMessage,
    PlayerSessionMessage,
    JoinGame,
    AtmosDisplayUpdate,
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

public enum AtomManagerMessage
{
    SpawnAtom,
    DeleteAtom,
    SetDrawDepth,
    Passthrough
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
    UIComponentMessage
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

