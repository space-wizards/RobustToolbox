using System;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Utility;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Robust.Client.UserInterface.Controls
{
    [ControlWrap("Button")]
    public class Button : BaseButton
    {
        public const string StylePropertyStyleBox = "stylebox";
        public const string StylePseudoClassNormal = "normal";
        public const string StylePseudoClassHover = "hover";
        public const string StylePseudoClassDisabled = "disabled";
        public const string StylePseudoClassPressed = "pressed";
        private int? _textWidthCache;

        public Button()
        {
        }

        public Button(string name) : base(name)
        {
        }

        [ViewVariables]
        public AlignMode TextAlign { get; set; } = AlignMode.Center;

        [ViewVariables]
        public bool ClipText { get; set; }

        private string _text;

        [ViewVariables]
        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                _textWidthCache = null;
                MinimumSizeChanged();
            }
        }

        private StyleBox ActualStyleBox
        {
            get
            {
                if (TryGetStyleProperty(StylePropertyStyleBox, out StyleBox box))
                {
                    return box;
                }

                return UserInterfaceManager.ThemeDefaults.ButtonStyle;
            }
        }

        public Font ActualFont
        {
            get
            {
                if (TryGetStyleProperty("font", out Font font))
                {
                    return font;
                }

                return UserInterfaceManager.ThemeDefaults.DefaultFont;
            }
        }

        public Color ActualFontColor
        {
            get
            {
                if (TryGetStyleProperty("font-color", out Color fontColor))
                {
                    return fontColor;
                }

                return FontColorOverride ?? Color.White;
            }
        }

        public Color? FontColorOverride { get; set; }
        public Color? FontColorDisabledOverride { get; set; }
        public Color? FontColorHoverOverride { get; set; }
        public Color? FontColorPressedOverride { get; set; }

        public enum AlignMode
        {
            Left = 0,
            Center = 1,
            Right = 2,
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            var style = ActualStyleBox;
            var drawBox = PixelSizeBox;
            style.Draw(handle, drawBox);

            if (_text == null)
            {
                return;
            }

            var contentBox = style.GetContentBox(drawBox);
            DrawTextInternal(handle, contentBox);
        }

        protected void DrawTextInternal(DrawingHandleScreen handle, UIBox2 box)
        {
            var width = EnsureWidthCache();
            var font = ActualFont;
            int drawOffset;

            if (box.Width < width)
            {
                drawOffset = 0;
            }
            else
            {
                switch (TextAlign)
                {
                    case AlignMode.Left:
                        drawOffset = 0;
                        break;
                    case AlignMode.Center:
                        drawOffset = (int) (box.Width - width) / 2;
                        break;
                    case AlignMode.Right:
                        drawOffset = (int) (box.Width - width);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            var color = ActualFontColor;
            var offsetY = (int) (box.Height - font.GetHeight(UIScale)) / 2;
            var baseLine = new Vector2i(drawOffset, offsetY + font.GetAscent(UIScale)) + box.TopLeft;

            foreach (var chr in _text)
            {
                if (!font.TryGetCharMetrics(chr, UIScale, out var metrics))
                {
                    continue;
                }

                if (!(ClipText && (baseLine.X < box.Left || baseLine.X + metrics.Advance > box.Right)))
                {
                    font.DrawChar(handle, chr, baseLine, UIScale, color);
                }

                baseLine += (metrics.Advance, 0);
            }
        }

        protected override Vector2 CalculateMinimumSize()
        {
            var style = ActualStyleBox;
            var font = ActualFont;

            var fontHeight = font.GetHeight(UIScale) / UIScale;

            if (ClipText)
            {
                return (0, fontHeight) + style.MinimumSize/UIScale;
            }

            var width = EnsureWidthCache();

            return (width / UIScale, fontHeight) + style.MinimumSize/UIScale;
        }

        protected override void Initialize()
        {
            base.Initialize();

            DrawModeChanged();
        }

        protected override void DrawModeChanged()
        {
            switch (DrawMode)
            {
                case DrawModeEnum.Normal:
                    StylePseudoClass = StylePseudoClassNormal;
                    break;
                case DrawModeEnum.Pressed:
                    StylePseudoClass = StylePseudoClassPressed;
                    break;
                case DrawModeEnum.Hover:
                    StylePseudoClass = StylePseudoClassHover;
                    break;
                case DrawModeEnum.Disabled:
                    StylePseudoClass = StylePseudoClassDisabled;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected int EnsureWidthCache()
        {
            if (_textWidthCache.HasValue)
            {
                return _textWidthCache.Value;
            }

            if (_text == null)
            {
                _textWidthCache = 0;
                return 0;
            }

            var font = ActualFont;

            var textWidth = 0;
            foreach (var chr in _text)
            {
                var metrics = font.GetCharMetrics(chr, UIScale);
                if (metrics == null)
                {
                    continue;
                }

                textWidth += metrics.Value.Advance;
            }

            _textWidthCache = textWidth;
            return textWidth;
        }

        protected override void StylePropertiesChanged()
        {
            _textWidthCache = null;

            base.StylePropertiesChanged();
        }

        private protected override void SetGodotProperty(string property, object value, GodotAssetScene context)
        {
            base.SetGodotProperty(property, value, context);

            if (property == "text")
            {
                Text = (string) value;
            }

            if (property == "align")
            {
                TextAlign = (AlignMode) (long) value;
            }

            if (property == "clip_text")
            {
                ClipText = (bool) value;
            }
        }

        protected internal override void UIScaleChanged()
        {
            _textWidthCache = null;

            base.UIScaleChanged();
        }
    }
}
