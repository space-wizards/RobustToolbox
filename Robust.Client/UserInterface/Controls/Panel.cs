using Robust.Client.Graphics.Drawing;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.Controls
{
    [ControlWrap("Panel")]
    public class Panel : Control
    {
        public const string StylePropertyPanel = "panel";

        public Panel(string name) : base(name)
        {
        }

        public Panel()
        {
        }

        private StyleBox _panelOverride;

        public StyleBox PanelOverride
        {
            get => _panelOverride;
            set => _panelOverride = value;
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

            var panel = ActualPanel;
            panel.Draw(handle, PixelSizeBox);
        }

        protected override Vector2 CalculateMinimumSize()
        {
            return ActualPanel.MinimumSize/UIScale;
        }
    }
}
