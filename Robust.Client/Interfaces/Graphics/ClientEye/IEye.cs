using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Client.Interfaces.Graphics.ClientEye
{
    /// <summary>
    ///     An Eye is a point through which the player can view the world.
    ///     It's a 2D camera in other game dev lingo basically.
    /// </summary>
    public interface IEye
    {
        /// <summary>
        ///     Whether this is the current eye. If true, this one will be used.
        /// </summary>
        bool Current { get; set; }

        Vector2 Zoom { get; set; }
        MapCoordinates Position { get; }
    }
}
