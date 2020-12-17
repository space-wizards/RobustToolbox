namespace Robust.Client.Interfaces.Graphics
{
    /// <summary>
    ///     OS-standard cursor shapes.
    /// </summary>
    public enum StandardCursorShape : byte
    {
        /// <summary>
        /// The standard arrow shape. Used in almost all situations.
        /// </summary>
        Arrow,

        /// <summary>
        /// The I-Beam shape. Used when mousing over a place where text can be entered.
        /// </summary>
        IBeam,

        /// <summary>
        /// The crosshair shape. Used when dragging and dropping.
        /// </summary>
        Crosshair,

        /// <summary>
        /// The hand shape. Used when mousing over something that can be dragged around.
        /// </summary>
        Hand,

        /// <summary>
        /// The horizontal resize shape. Used when mousing over something that can be horizontally resized.
        /// </summary>
        HResize,

        /// <summary>
        /// The vertical resize shape. Used when mousing over something that can be vertically resized.
        /// </summary>
        VResize
    }
}
