using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Robust.Client.UserInterface.CustomControls
{
    public sealed class Tooltip : PanelContainer
    {
        private readonly Label _label;

        public string? Text
        {
            get => _label.Text;
            set => _label.Text = value;
        }

        /// <summary>
        /// Should we track the mouse cursor.
        /// </summary>
        public bool Tracking = false;

        public Tooltip()
        {
            var vbox = new BoxContainer()
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                RectClipContent = true,
            };

            AddChild(vbox);

            vbox.AddChild(_label = new Label());
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            if (!Tracking)
                return;

            Tooltips.PositionTooltip(this);
        }
    }
}
