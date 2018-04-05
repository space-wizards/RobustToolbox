using SS14.Shared.Map;

namespace SS14.Client.Interfaces.Graphics.ClientEye
{
    /// <summary>
    ///     An Eye is a point through which the player can view the world.
    ///     It's a 2D camera in other game dev lingo basically.
    /// </summary>
    public interface IEye
    {
        Godot.Camera2D GodotCamera { get; }

        MapId MapId { get; }

        /// <summary>
        ///     Whether this is the current eye. If true, this one will be used.
        /// </summary>
        bool Current { get; set; }
    }
}
