using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.Controls
{
    public class RichTextLabel : Control
    {
        private FormattedMessage? _message;
        private RichTextEntry _entry;

        public float? MaxWidth { get; set; }

        public void SetMessage(FormattedMessage message)
        {
            _message = message;
            _entry = new RichTextEntry(_message);
            _updateEntry();
        }

        public void SetMessage(string message)
        {
            var msg = new FormattedMessage();
            msg.AddText(message);
            SetMessage(msg);
        }

        protected override Vector2 CalculateMinimumSize()
        {
            if (_message == null)
            {
                return Vector2.Zero;
            }

            var width = 0f;
            if (MaxWidth.HasValue)
            {
                width = _entry.Width / UIScale;
            }
            return (width, _entry.Height / UIScale);
        }

        private void _updateEntry()
        {
            var font = _getFont();

            if (_message != null)
            {
                var oldHeight = _entry.Height;
                var oldWidth = _entry.Width;
                _entry.Update(font, (MaxWidth ?? Width) * UIScale, UIScale);
                if (oldHeight != _entry.Height || MaxWidth != null && _entry.Width != oldWidth)
                {
                    MinimumSizeChanged();
                }
            }
        }

        protected override void StylePropertiesChanged()
        {
            base.StylePropertiesChanged();

            _updateEntry();
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
            if (TryGetStyleProperty<Font>("font", out var font))
            {
                return font;
            }

            return UserInterfaceManager.ThemeDefaults.DefaultFont;
        }
    }
}
