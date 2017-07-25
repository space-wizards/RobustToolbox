using SFML.System;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;

namespace SS14.Client.Interfaces.GameObjects.Components
{
    public interface IClientClickableComponent : IClickableComponent
    {
        /// <summary>
        /// Used to check whether a click worked.
        /// </summary>
        /// <param name="worldPos">The world position that was clicked.</param>
        /// <param name="drawdepth">The draw depth for the click. On what depth did the click go through.</param>
        /// <returns>True if the click worked, false otherwise.</returns>
        bool CheckClick(Vector2f worldPos, out int drawdepth);

        /// <summary>
        /// Sends the click to the sister component on the server and things subscribed to
        /// </summary>
        /// <param name="userUID">The entity owned by the player that clicked.</param>
        /// <param name="clickType">See <see cref="MouseClickType" />.</param>
        void DispatchClick(IEntity user, int clickType);
    }
}
