namespace Robust.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.VScrollBar))]
    public class VScrollBar : ScrollBar
    {
        public VScrollBar() : base(OrientationMode.Vertical)
        {
        }

        internal VScrollBar(Godot.ScrollBar control) : base(control)
        {
        }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.VScrollBar();
        }
    }
}
