namespace Robust.Client.Graphics
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
        /// Alias for <see cref="IBeam"/>.
        /// </summary>
        Text = IBeam,

        /// <summary>
        /// The crosshair shape. Used when dragging and dropping.
        /// </summary>
        Crosshair,

        /// <summary>
        /// The hand shape. Used when mousing over something that can be dragged around.
        /// </summary>
        Hand,

        /// <summary>
        /// Alias for <see cref="Hand"/>
        /// </summary>
        Pointer = Hand,

        /// <summary>
        /// The horizontal resize shape. Used when mousing over something that can be horizontally resized.
        /// </summary>
        HResize,

        /// <summary>
        /// Alias for <see cref="EWResize"/>
        /// </summary>
        EWResize = HResize,

        /// <summary>
        /// The vertical resize shape. Used when mousing over something that can be vertically resized.
        /// </summary>
        VResize,

        /// <summary>
        /// Alias for <see cref="VResize"/>.
        /// </summary>
        NSResize = VResize,

        /// <summary>
        /// Program is busy doing something.
        /// </summary>
        /// <remarks>
        /// This cursor is not always available and may be substituted.
        /// </remarks>
        Progress,

        /// <summary>
        /// Diagonal resize shape for northwest-southeast resizing.
        /// </summary>
        /// <remarks>
        /// This cursor is not always available and may be substituted.
        /// </remarks>
        NWSEResize,

        /// <summary>
        /// Diagonal resize shape for northeast-southwest resizing.
        /// </summary>
        /// <remarks>
        /// This cursor is not always available and may be substituted.
        /// </remarks>
        NESWResize,

        /// <summary>
        /// 4-way arrow move icon.
        /// </summary>
        /// <remarks>
        /// This cursor is not always available and may be substituted.
        /// </remarks>
        Move,

        /// <summary>
        /// An action is not allowed.
        /// </summary>
        /// <remarks>
        /// This cursor is not always available and may be substituted.
        /// </remarks>
        NotAllowed,

        /// <summary>
        /// One-directional resize to the northwest.
        /// </summary>
        /// <remarks>
        /// This cursor is not always available and may be substituted.
        /// </remarks>
        NWResize,

        /// <summary>
        /// One-directional resize to the north.
        /// </summary>
        /// <remarks>
        /// This cursor is not always available and may be substituted.
        /// </remarks>
        NResize,

        /// <summary>
        /// One-directional resize to the northeast.
        /// </summary>
        /// <remarks>
        /// This cursor is not always available and may be substituted.
        /// </remarks>
        NEResize,

        /// <summary>
        /// One-directional resize to the east.
        /// </summary>
        /// <remarks>
        /// This cursor is not always available and may be substituted.
        /// </remarks>
        EResize,

        /// <summary>
        /// One-directional resize to the southeast.
        /// </summary>
        /// <remarks>
        /// This cursor is not always available and may be substituted.
        /// </remarks>
        SEResize,

        /// <summary>
        /// One-directional resize to the south.
        /// </summary>
        /// <remarks>
        /// This cursor is not always available and may be substituted.
        /// </remarks>
        SResize,

        /// <summary>
        /// One-directional resize to the southwest.
        /// </summary>
        /// <remarks>
        /// This cursor is not always available and may be substituted.
        /// </remarks>
        SWResize,

        /// <summary>
        /// One-directional resize to the west.
        /// </summary>
        /// <remarks>
        /// This cursor is not always available and may be substituted.
        /// </remarks>
        WResize,

        /// <summary>
        /// Not a real value
        /// </summary>
        CountCursors,
    }
}
