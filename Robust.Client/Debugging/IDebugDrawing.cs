namespace Robust.Client.Debugging
{
    /// <summary>
    /// A collection of visual debug overlays for the client game.
    /// </summary>
    public interface IDebugDrawing
    {
        /// <summary>
        /// Toggles the visual overlay of the local origin for each entity on screen.
        /// </summary>
        bool DebugPositions { get; set; }
    }
}
