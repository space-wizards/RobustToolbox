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
    ChangeTile,
    ItemMessage, // It's something the item system needs to handle
    MobMessage,
    ChatMessage,
    AtomManagerMessage,
    PlayerSessionMessage
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
    AttachTo
}

public enum AtomManagerMessage
{
    SpawnAtom,
    DeleteAtom,
    Passthrough
}

public enum MobMessage
{
    CreateMob = 0,
    InterpolationPacket,
    DeleteMob,
    ClickMob,
    AnimationState,
    SelectAppendage
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
    Verb
}
