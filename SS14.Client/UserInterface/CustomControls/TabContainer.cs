using SS14.Client.Graphics.Sprites;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.CustomControls
{
    internal class TabContainer : ScrollableContainer
    {
        /// <summary>
        ///     Sprite that is being used in the TabbedMenu.
        /// </summary>
        public Sprite TabSprite;

        /// <summary>
        ///     Path of the sprite that shows up in the TabbedMenu.
        /// </summary>
        public string TabSpriteName
        {
            set => TabSprite = _resourceCache.GetSprite(value);
        }

        /// <summary>
        ///     Creates an instance of this object.
        /// </summary>
        /// <param name="size">Dimensions of the container in px.</param>
        public TabContainer(Vector2i size)
            : base(size) { }

        /// <summary>
        ///     Called when tab is selected.
        /// </summary>
        public virtual void Activated() { }
    }
}
