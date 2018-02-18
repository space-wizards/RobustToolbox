using SS14.Client.State.States;
using SS14.Shared.Enums;
using SS14.Shared.GameObjects;
using SS14.Shared.Input;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Map;

namespace SS14.Client.Interfaces.GameObjects.Components
{
    public interface IClientClickableComponent : IClickableComponent
    {
        /// <summary>
        /// Used to check whether a click worked.
        /// </summary>
        /// <param name="worldPos">The world position that was clicked.</param>
        /// <param name="drawdepth">
        /// The draw depth for the sprite that captured the click.
        /// Used by <see cref="GameScreen" /> to sort and pick the highest successful one when multiple overlapping entities passed.
        /// </param>
        /// <returns>True if the click worked, false otherwise.</returns>
        bool CheckClick(LocalCoordinates worldPos, out int drawdepth);

        /// <summary>
        /// Sends the click to the sister component on the server and things subscribed to
        /// </summary>
        /// <param name="userUID">The entity owned by the player that clicked.</param>
        /// <param name="clickType">See <see cref="MouseClickType" />.</param>
        void DispatchClick(IEntity user, ClickType clickType);
    }
}
