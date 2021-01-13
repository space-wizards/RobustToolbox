namespace Robust.Shared.ContentPack
{
    /// <summary>
    ///     Levels at which point the content assemblies are getting updates.
    /// </summary>
    public enum ModUpdateLevel : byte
    {
        /// <summary>
        ///     This update is called before the main state manager on process frames.
        /// </summary>
        PreEngine,

        /// <summary>
        ///     This update is called before the main state manager on render frames, thus only applies to the client.
        /// </summary>
        FramePreEngine,

        /// <summary>
        ///     This update is called after the main state manager on process frames.
        /// </summary>
        PostEngine,

        /// <summary>
        ///     This update is called after the main state manager on render frames, thus only applies to the client.
        /// </summary>
        FramePostEngine,
    }
}
