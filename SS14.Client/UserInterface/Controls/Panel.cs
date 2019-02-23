using SS14.Client.Graphics.Drawing;
using SS14.Client.Utility;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.Panel))]
    public class Panel : Control
    {
        public const string StylePropertyPanel = "panel";

        public Panel(string name) : base(name)
        {
        }

        public Panel() : base()
        {
        }

        internal Panel(Godot.Panel panel) : base(panel)
        {
        }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.Panel();
        }

        private StyleBox _panelOverride;

        public StyleBox PanelOverride
        {
            get => _panelOverride ?? GetStyleBoxOverride("panel");
            set => SetStyleBoxOverride("panel", _panelOverride = value);
        }

        private StyleBox ActualPanel
        {
            get
            {
                if (_panelOverride != null)
                {
                    return _panelOverride;
                }

                if (TryGetStyleProperty(StylePropertyPanel, out StyleBox panel))
                {
                    return panel;
                }

                return UserInterfaceManager.ThemeDefaults.PanelPanel;
            }
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            if (!GameController.OnGodot)
            {
                var panel = ActualPanel;
                panel.Draw(handle, SizeBox);
            }
        }

        private protected override void SetGodotProperty(string property, object value, GodotAssetScene context)
        {
            base.SetGodotProperty(property, value, context);

            if (property == "custom_styles/panel")
            {
                PanelOverride = GetGodotResource<StyleBox>(context, value);
            }
        }

        protected override Vector2 CalculateMinimumSize()
        {
            if (GameController.OnGodot)
            {
                return Vector2.Zero;
            }

            return ActualPanel.MinimumSize;
        }
    }
}
