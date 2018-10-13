using SS14.Client.Graphics.Drawing;

namespace SS14.Client.UserInterface.Controls
{
    #if GODOT
    [ControlWrap(typeof(Godot.PanelContainer))]
    #endif
    public class PanelContainer : Control
    {
        public PanelContainer()
        {
        }

        public PanelContainer(string name) : base(name)
        {
        }

        #if GODOT
        internal PanelContainer(Godot.PanelContainer container) : base(container)
        {
        }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.PanelContainer();
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
