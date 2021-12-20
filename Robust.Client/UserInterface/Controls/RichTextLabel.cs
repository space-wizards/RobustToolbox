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

            _entry.Update(_getFont(), availableSize.X * UIScale, UIScale);

            return (_entry.Width / UIScale, _entry.Height / UIScale);
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            if (_message == null)
            {
                return;
            }

            _entry.Draw(handle, _getFont(), SizeBox, 0, UIScale, _getFontColor());
        }

        [Pure]
        private IFontLibrary _getFont()
        {
            TryGetStyleProperty<FontClass>("font", out var font);
            if (TryGetStyleProperty<IFontLibrary>("font-library", out var flib))
            {
                return flib;
            }

            return UserInterfaceManager
                .ThemeDefaults
                .DefaultFontLibrary;
        }

        [Pure]
        private Color _getFontColor()
        {
            if (TryGetStyleProperty<Color>("font-color", out var fc))
                return fc;

            // From Robust.Client/UserInterface/RichTextEntry.cs#L19
            // at 33008a2bce0cc4755b18b12edfaf5b6f1f87fdd9
            return new Color(200, 200, 200);
        }
    }
}
