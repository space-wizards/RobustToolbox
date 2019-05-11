using System;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Utility;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     A label is a GUI control that displays simple text.
    /// </summary>
    [ControlWrap("Label")]
    public class Label : Control
    {
        public const string StylePropertyFontColor = "font-color";
        public const string StylePropertyFont = "font";

        private Vector2i? _textDimensionCache;

        public Label(string name) : base(name)
        {
        }

        public Label()
        {
        }

        private string _text;

        [ViewVariables]
        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                _textDimensionCache = null;
                MinimumSizeChanged();
            }
        }

        [ViewVariables]
        public AlignMode Align { get; set; }

        [ViewVariables]
        public VAlignMode VAlign { get; set; }

        public Font FontOverride { get; set; }

        private Font ActualFont
        {
            get
            {
                if (FontOverride != null)
                {
                    return FontOverride;
                }

                if (TryGetStyleProperty<Font>(StylePropertyFont, out var font))
                {
                    return font;
                }

                return UserInterfaceManager.ThemeDefaults.LabelFont;
            }
        }

        public Color? FontColorShadowOverride { get; set; }

        private Color ActualFontColor
        {
            get
            {
                if (FontColorOverride.HasValue)
                {
                    return FontColorOverride.Value;
                }

                if (TryGetStyleProperty<Color>(StylePropertyFontColor, out var color))
                {
                    return color;
                }

                return Color.White;
            }
        }

        public Color? FontColorOverride { get; set; }

        public int? ShadowOffsetXOverride { get; set; }

        public int? ShadowOffsetYOverride { get; set; }


        protected internal override void Draw(DrawingHandleScreen handle)
        {
            if (_text == null)
            {
                return;
            }

            if (!_textDimensionCache.HasValue)
            {
                _calculateTextDimension();
                DebugTools.Assert(_textDimensionCache.HasValue);
            }

            int hOffset;
            switch (Align)
            {
                case AlignMode.Left:
                    hOffset = 0;
                    break;
                case AlignMode.Center:
                case AlignMode.Fill:
                    hOffset = (PixelSize.X - _textDimensionCache.Value.X) / 2;
                    break;
                case AlignMode.Right:
                    hOffset = PixelSize.X - _textDimensionCache.Value.X;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            int vOffset;
            switch (VAlign)
            {
                case VAlignMode.Top:
                    vOffset = 0;
                    break;
                case VAlignMode.Fill:
                case VAlignMode.Center:
                    vOffset = (PixelSize.Y - _textDimensionCache.Value.Y) / 2;
                    break;
                case VAlignMode.Bottom:
                    vOffset = PixelSize.Y - _textDimensionCache.Value.Y;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var newlines = 0;
            var font = ActualFont;
            var baseLine = new Vector2(hOffset, font.GetAscent(UIScale) + vOffset);
            var actualFontColor = ActualFontColor;
            foreach (var chr in _text)
            {
                if (chr == '\n')
                {
                    newlines += 1;
                    baseLine = new Vector2(hOffset, font.GetAscent(UIScale) + font.GetLineHeight(UIScale) * newlines);
                }

                var advance = font.DrawChar(handle, chr, baseLine, UIScale, actualFontColor);
                baseLine += new Vector2(advance, 0);
            }
        }

        public enum AlignMode
        {
            Left = 0,
            Center = 1,
            Right = 2,
            Fill = 3,
        }

        public enum VAlignMode
        {
            Top = 0,
            Center = 1,
            Bottom = 2,
            Fill = 3,
        }

        protected override Vector2 CalculateMinimumSize()
        {
            if (!_textDimensionCache.HasValue)
            {
                _calculateTextDimension();
                DebugTools.Assert(_textDimensionCache.HasValue);
            }

            return _textDimensionCache.Value;
        }

        protected internal override void UIScaleChanged()
        {
            _textDimensionCache = null;

            base.UIScaleChanged();
        }

        private void _calculateTextDimension()
        {
            if (_text == null)
            {
                _textDimensionCache = Vector2i.Zero;
                return;
            }

            var font = ActualFont;
            var height = font.GetHeight(UIScale);
            var maxLineSize = 0;
            var currentLineSize = 0;
            foreach (var chr in _text)
            {
                if (chr == '\n')
                {
                    maxLineSize = Math.Max(currentLineSize, maxLineSize);
                    currentLineSize = 0;
                    height += font.GetLineHeight(UIScale);
                }
                else
                {
                    var metrics = font.GetCharMetrics(chr, UIScale);
                    if (!metrics.HasValue)
                    {
                        continue;
                    }

                    currentLineSize += metrics.Value.Advance;
                }
            }

            maxLineSize = Math.Max(currentLineSize, maxLineSize);

            _textDimensionCache = (Vector2i)(new Vector2(maxLineSize, height) / UIScale);
        }

        protected override void StylePropertiesChanged()
        {
            _textDimensionCache = null;

            base.StylePropertiesChanged();
        }

        private protected override void SetGodotProperty(string property, object value, GodotAssetScene context)
        {
            base.SetGodotProperty(property, value, context);

            switch (property)
            {
                case "text":
                    Text = (string) value;
                    break;
                case "align":
                    Align = (AlignMode) (long) value;
                    break;
                // ReSharper disable once StringLiteralTypo
                case "valign":
                    VAlign = (VAlignMode) (long) value;
                    break;
            }
        }

        protected override void SetDefaults()
        {
            base.SetDefaults();
            MouseFilter = MouseFilterMode.Ignore;
            SizeFlagsVertical = SizeFlags.ShrinkCenter;
        }
    }
}
