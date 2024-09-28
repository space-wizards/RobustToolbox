using System;
using System.Numerics;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Client.UserInterface.Controls
{
    [Virtual]
    public class RichTextLabel : Control
    {
        [Dependency] private readonly MarkupTagManager _tagManager = default!;

        private FormattedMessage? _message;
        private RichTextEntry _entry;
        private float _lineHeightScale = 1;
        private bool _lineHeightOverride;

        [ViewVariables(VVAccess.ReadWrite)]
        public float LineHeightScale
        {
            get
            {
                if (!_lineHeightOverride && TryGetStyleProperty(nameof(LineHeightScale), out float value))
                    return value;

                return _lineHeightScale;
            }
            set
            {
                _lineHeightScale = value;
                _lineHeightOverride = true;
                InvalidateMeasure();
            }
        }

        public string? Text
        {
            get => _message?.ToMarkup();
            set
            {
                if (value == null)
                {
                    _message?.Clear();
                    return;
                }

                SetMessage(FormattedMessage.FromMarkupPermissive(value));
            }
        }

        public RichTextLabel()
        {
            IoCManager.InjectDependencies(this);
            VerticalAlignment = VAlignment.Center;
        }

        public void SetMessage(FormattedMessage message, Type[]? tagsAllowed = null, Color? defaultColor = null)
        {
            _message = message;
            _entry = new RichTextEntry(_message, this, _tagManager, tagsAllowed, defaultColor);
            InvalidateMeasure();
        }

        public void SetMessage(string message, Type[]? tagsAllowed = null, Color? defaultColor = null)
        {
            var msg = new FormattedMessage();
            msg.AddText(message);
            SetMessage(msg, tagsAllowed, defaultColor);
        }

        public string? GetMessage() => _message?.ToMarkup();

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            if (_message == null)
            {
                return Vector2.Zero;
            }

            var font = _getFont();
            _entry.Update(_tagManager, font, availableSize.X * UIScale, UIScale, LineHeightScale);

            return new Vector2(_entry.Width / UIScale, _entry.Height / UIScale);
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            if (_message == null)
            {
                return;
            }

            _entry.Draw(_tagManager, handle, _getFont(), SizeBox, 0, new MarkupDrawingContext(), UIScale, LineHeightScale);
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
