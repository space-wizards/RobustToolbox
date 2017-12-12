namespace SS14.Shared
{
    public enum SessionStatus : byte
    {
        Zombie = 0,
        Connected,
        InLobby,
        InGame,
        Disconnected
    }
}
