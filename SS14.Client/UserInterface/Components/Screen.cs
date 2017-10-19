using SS14.Client.UserInterface.Components;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface
{
    /// <summary>
    ///     UI Base screen that holds all of the other controls.
    /// </summary>
    public class Screen : Control
    {
        public Screen()
        {
            // if this is disabled, you will prob see the gl clear color.
            DrawBackground = true;
        }

        /// <inheritdoc />
        protected override void OnCalcRect()
        {
            _clientArea = Box2i.FromDimensions(0, 0, Width, Height);
        }
    }
}
