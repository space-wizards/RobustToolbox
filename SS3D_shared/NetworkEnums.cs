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
    MobMessage
}

public enum ItemMessage
{
    CreateItem = 0,
    InterpolationPacket,
    UseItem // etc.
}

public enum MobMessage
{
    CreateMob = 0,
    InterpolationPacket,
    DeleteMob
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