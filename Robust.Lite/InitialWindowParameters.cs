using Robust.Shared.Maths;

namespace Robust.Lite
{
    /// <summary>
    ///     Initial parameters for instantiating the window.
    /// </summary>
    public class InitialWindowParameters
    {
        /// <summary>
        ///     The title of the window. This string will be localized before being used.
        /// </summary>
        public string WindowTitle { get; set; }

        /// <summary>
        ///     The initial size of the window.
        /// </summary>
        public Vector2i? Size { get; set; }
    }
}
