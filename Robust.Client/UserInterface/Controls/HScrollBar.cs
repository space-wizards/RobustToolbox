namespace Robust.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.HScrollBar))]
    public class HScrollBar : ScrollBar
    {
        public HScrollBar() : base(OrientationMode.Horizontal)
        {
        }

        internal HScrollBar(Godot.HScrollBar control) : base(control)
        {
        }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.HScrollBar();
        }
    }
}
