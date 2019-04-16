using Robust.Client.State.States;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Map;

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
        /// Used by <see cref="GameScreen" /> to sort and pick the highest successful one when multiple overlapping entities passed.
        /// </param>
        /// <returns>True if the click worked, false otherwise.</returns>
        bool CheckClick(GridCoordinates worldPos, out int drawdepth);

        /// <summary>
        /// Sends the click to the sister component on the server and things subscribed to
        /// </summary>
        /// <param name="userUID">The entity owned by the player that clicked.</param>
        /// <param name="clickType">See <see cref="MouseClickType" />.</param>
        void DispatchClick(IEntity user, ClickType clickType);

        /// <summary>
        ///     Invoked whenever the mouse hovers over this entity.
        /// </summary>
        void OnMouseEnter();

        /// <summary>
        ///     Invoked whenever the mouse stops hovering over this entity.
        /// </summary>
        void OnMouseLeave();
    }
}
