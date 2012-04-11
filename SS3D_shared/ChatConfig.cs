namespace SS13_Shared
{
    public enum ChatChannel
    {
        Default,
        Lobby, // Players in the lobby chat on this channel.
        Ingame, // This is local chat.
        Server, // Messages from the server
        Damage, // Damage messages
        Player, // Messages that are sent by the player
        Radio, // Radio messages
        Emote, // Emotes
        OOC, // Out-of-character channel
        Visual, // Things the character can see
    }
}