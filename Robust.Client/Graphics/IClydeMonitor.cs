using System.Collections.Generic;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics
{
    /// <summary>
    ///     Represents a connected monitor on the user's system.
    /// </summary>
    public interface IClydeMonitor
    {
        /// <summary>
        ///     This ID is not consistent between startups of the game.
        /// </summary>
        int Id { get; }
        string Name { get; }
        Vector2i Size { get; }
        int RefreshRate { get; }

        IEnumerable<VideoMode> VideoModes { get; }
    }
}
