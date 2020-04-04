using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Maths;

namespace Robust.Client.Interfaces.GameObjects.Components
{
    public interface IClientClickableComponent : IClickableComponent
    {
        /// <summary>
        /// Used to check whether a click worked.
        /// </summary>
        /// <param name="worldPos">The world position that was clicked.</param>
        /// <param name="drawdepth">
        /// The draw depth for the sprite that captured the click.
        /// </param>
        /// <returns>True if the click worked, false otherwise.</returns>
        bool CheckClick(Vector2 worldPos, out int drawdepth);
    }
}
