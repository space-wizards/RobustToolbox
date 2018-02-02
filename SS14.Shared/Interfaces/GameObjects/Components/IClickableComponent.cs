using SS14.Shared.GameObjects;
using System;

namespace SS14.Shared.Interfaces.GameObjects.Components
{
    public interface IClickableComponent : IComponent
    {
        /// <summary>
        /// Invoked whenever this component is clicked on.
        /// </summary>
        event EventHandler<ClickEventArgs> OnClick;
    }

    public class ClickEventArgs : EventArgs
    {
        /// <summary>
        /// The mob that did the click.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        /// The entity that got clicked.
        /// </summary>
        public IEntity Source { get; }

        // TODO: refactor this.
        // Needs a more sane way to write non-primitive network messages (see issue #288)
        // Use some struct to store this and probably some bitmap for the modifier keys.
        /// <summary>
        /// The type of mouse click. See the constants in <see cref="MouseClickType" /> for what this value means.
        /// </summary>
        public ClickType ClickType { get; }

        public ClickEventArgs(IEntity user, IEntity source, ClickType clickType)
        {
            User = user;
            Source = source;
            ClickType = clickType;
        }
    }
}
