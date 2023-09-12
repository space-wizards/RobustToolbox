using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.CustomControls
{
    public sealed class Tooltip : PanelContainer
    {
        private readonly RichTextLabel _label;

        public string? Text
        {
            get => _label.GetMessage();
            set
            {
                if (value == null)
                {
                    _label.SetMessage(string.Empty);
                }
                else
                {
                    _label.SetMessage(value);
                }
            }
        }

        public void SetMessage(FormattedMessage message)
        {
            _label.SetMessage(message);
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

            vbox.AddChild(_label = new RichTextLabel());
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
