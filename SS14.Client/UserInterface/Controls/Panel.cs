using SS14.Client.Graphics.Drawing;

namespace SS14.Client.UserInterface.Controls
{
    #if GODOT
    [ControlWrap(typeof(Godot.Panel))]
    #endif
    public class Panel : Control
    {
        public Panel(string name) : base(name)
        {
        }
        public Panel() : base()
        {
        }
        #if GODOT
        internal Panel(Godot.Panel panel) : base(panel)
        {
        }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.Panel();
        }
        #endif

        private StyleBox _panelOverride;

        public StyleBox PanelOverride
        {
            get => _panelOverride ?? GetStyleBoxOverride("panel");
            set => SetStyleBoxOverride("panel", _panelOverride = value);
        }
    }
}
