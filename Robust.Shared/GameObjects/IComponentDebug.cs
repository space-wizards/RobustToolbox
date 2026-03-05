namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     An ages old interface to add extra debug data to the "entfo" server-side command.
    /// </summary>
    public partial interface IComponentDebug : IComponent
    {
        /// <summary>
        ///     Returns a debug string to print to the console or otherwise display from debug tools.
        ///     This can contain newlines.
        /// </summary>
        string GetDebugString();
    }
}
