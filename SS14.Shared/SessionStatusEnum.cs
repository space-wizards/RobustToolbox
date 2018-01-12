namespace SS14.Shared
{
    public enum SessionStatus : byte
    {
        Zombie = 0,
        Connecting,
        Connected,
        InLobby,
        InGame,
        Disconnected
    }
}
