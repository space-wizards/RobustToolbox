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

        public void SetMessage(FormattedMessage message)
        {
            _message = message;
            _entry = new RichTextEntry(_message);
            InvalidateMeasure();
        }

        public void SetMessage(string message)
        {
            var msg = new FormattedMessage.Builder();
            msg.AddText(message);
            SetMessage(msg.Build());
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            if (_message == null)
            {
                return Vector2.Zero;
            }

            var font = _getFont();
            _entry.Update(font, availableSize.X * UIScale, UIScale);

            return (_entry.Width / UIScale, _entry.Height / UIScale);
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            if (_message == null)
            {
                return;
            }

#if false
            _entry.Draw(handle, _getFont(), SizeBox, 0, new Stack<FormattedMessage.Tag>(), UIScale);
#endif
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
