using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Controls
{
    /// <summary>
    ///     A container for other controls.
    /// </summary>
    public class Panel : Control
    {
        public Panel()
        {
            DrawBackground = true;
        }

        protected override void OnCalcRect()
        {
            _clientArea = Box2i.FromDimensions(0, 0, Width, Height);
        }
    }
}
