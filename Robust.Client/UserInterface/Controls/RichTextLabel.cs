using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.Controls
{
    public class RichTextLabel : Control
    {
        private FormattedMessage _message;
        private RichTextEntry _entry;

        public RichTextLabel()
        {
        }

        public RichTextLabel(string name) : base(name)
        {
        }

        public void SetMessage(FormattedMessage message)
        {
            _message = message;
            _entry = new RichTextEntry(_message);
            _updateEntry();
        }

        protected override Vector2 CalculateMinimumSize()
        {
            if (_message == null)
            {
                return Vector2.Zero;
            }

            return (0, _entry.Height / UIScale);
        }

        private void _updateEntry()
        {
            var font = _getFont();

            if (_message != null)
            {
                var oldHeight = _entry.Height;
                _entry.Update(font, Width, UIScale);
                if (oldHeight != _entry.Height)
                {
                    MinimumSizeChanged();
                }
            }
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            if (_message == null)
            {
                return;
            }

            _entry.Draw(handle, _getFont(), SizeBox, 0, new Stack<FormattedMessage.Tag>(), UIScale);
        }

        protected override void Resized()
        {
            base.Resized();

            _updateEntry();
        }

        [Pure]
        private Font _getFont()
        {
            if (TryGetStyleProperty("font", out Font font))
            {
                return font;
            }

            return UserInterfaceManager.ThemeDefaults.DefaultFont;
        }
    }
}
