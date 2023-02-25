using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.Controls
{
    [Virtual]
    public class RichTextLabel : Control
    {
        [Dependency] private readonly MarkupTagManager _tagManager = default!;

        private FormattedMessage? _message;
        private RichTextEntry _entry;

        public RichTextLabel()
        {
            IoCManager.InjectDependencies(this);
        }

        public void SetMessage(FormattedMessage message)
        {
            _message = message;
            _entry = new RichTextEntry(_message, this, _tagManager);
            InvalidateMeasure();
        }

        public void SetMessage(string message)
        {
            var msg = new FormattedMessage();
            msg.AddText(message);
            SetMessage(msg);
        }

        public string? GetMessage() => _message?.ToMarkup();

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

            _entry.Draw(handle, _getFont(), SizeBox, 0, new MarkupDrawingContext(), UIScale);
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
