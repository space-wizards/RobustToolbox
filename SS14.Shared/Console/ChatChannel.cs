namespace SS14.Shared.Console
{
    public enum ChatChannel
    {
        /// <summary>
        ///     Default, unspecified
        /// </summary>
        Default = 0,

        /// <summary>
        ///     Chat heard by players within earshot
        /// </summary>
        Local,

        /// <summary>
        ///     Messages from the server
        /// </summary>
        Server,

        /// <summary>
        ///     Damage messages
        /// </summary>
        Damage,

        /// <summary>
        ///     Messages that are sent by the player directly
        /// </summary>
        Player,

        /// <summary>
        ///     Radio messages
        /// </summary>
        Radio,

        /// <summary>
        ///     Emotes
        /// </summary>
        Emote,

        /// <summary>
        ///     Out-of-character channel
        /// </summary>
        OOC,

        /// <summary>
        ///     Things the character can see
        /// </summary>
        Visual
    }
}
