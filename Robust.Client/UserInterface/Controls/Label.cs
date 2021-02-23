using System;
using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Shared.Animations;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     A label is a GUI control that displays simple text.
    /// </summary>
    public class Label : Control
    {
        public const string StylePropertyFontColor = "font-color";
        public const string StylePropertyFont = "font";
        public const string StylePropertyAlignMode = "alignMode";

        private int _cachedTextHeight;
        private readonly List<int> _cachedTextWidths = new();
        private bool _textDimensionCacheValid;
        private string? _text;
        private bool _clipText;
        private AlignMode _align;

        public Label()
        {
            VerticalAlignment = VAlignment.Center;
        }

        /// <summary>
        ///     The text to display.
        /// </summary>
        [ViewVariables]
        public string? Text
        {
            get => _text;
            set
            {
                _text = value;
                _textDimensionCacheValid = false;
                InvalidateMeasure();
            }
        }

        [ViewVariables]
        public bool ClipText
        {
            get => _clipText;
            set
            {
                _clipText = value;
                RectClipContent = value;
                InvalidateMeasure();
            }
        }

        [ViewVariables] public AlignMode Align {
            get
            {
                if (TryGetStyleProperty<AlignMode>(StylePropertyAlignMode, out var alignMode))
                {
                    return alignMode;
                }

                return _align;
            }
            set => _align = value;
        }

        [ViewVariables] public VAlignMode VAlign { get; set; }

        public Font? FontOverride { get; set; }

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

        [ViewVariables(VVAccess.ReadWrite)]
        [Animatable]
        public Color? FontColorOverride { get; set; }

        public int? ShadowOffsetXOverride { get; set; }

        public int? ShadowOffsetYOverride { get; set; }


        protected internal override void Draw(DrawingHandleScreen handle)
        {
            if (_text == null)
            {
                return;
            }

            if (!_textDimensionCacheValid)
            {
                _calculateTextDimension();
                DebugTools.Assert(_textDimensionCacheValid);
            }

            int vOffset;
            switch (VAlign)
            {
                case VAlignMode.Top:
                    vOffset = 0;
                    break;
                case VAlignMode.Fill:
                case VAlignMode.Center:
                    vOffset = (PixelSize.Y - _cachedTextHeight) / 2;
                    break;
                case VAlignMode.Bottom:
                    vOffset = PixelSize.Y - _cachedTextHeight;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var newlines = 0;
            var font = ActualFont;
            var actualFontColor = ActualFontColor;

            Vector2 CalcBaseline()
            {
                DebugTools.Assert(_textDimensionCacheValid);

                int hOffset;
                switch (Align)
                {
                    case AlignMode.Left:
                        hOffset = 0;
                        break;
                    case AlignMode.Center:
                    case AlignMode.Fill:
                        hOffset = (PixelSize.X - _cachedTextWidths[newlines]) / 2;
                        break;
                    case AlignMode.Right:
                        hOffset = PixelSize.X - _cachedTextWidths[newlines];
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return (hOffset, font.GetAscent(UIScale) + font.GetLineHeight(UIScale) * newlines + vOffset);
            }

            var baseLine = CalcBaseline();

            foreach (var chr in _text)
            {
                if (chr == '\n')
                {
                    newlines += 1;
                    baseLine = CalcBaseline();
                }

                var advance = font.DrawChar(handle, chr, baseLine, UIScale, actualFontColor);
                baseLine += (advance, 0);
            }
        }

        public enum AlignMode : byte
        {
            Left = 0,
            Center = 1,
            Right = 2,
            Fill = 3
        }

        public enum VAlignMode : byte
        {
            Top = 0,
            Center = 1,
            Bottom = 2,
            Fill = 3
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            if (!_textDimensionCacheValid)
            {
                _calculateTextDimension();
                DebugTools.Assert(_textDimensionCacheValid);
            }

            if (ClipText)
            {
                return (0, _cachedTextHeight / UIScale);
            }

            var totalWidth = 0;
            foreach (var width in _cachedTextWidths)
            {
                totalWidth = Math.Max(totalWidth, width);
            }

            return (totalWidth / UIScale, _cachedTextHeight / UIScale);
        }

        protected internal override void UIScaleChanged()
        {
            _textDimensionCacheValid = false;

            base.UIScaleChanged();
        }

        private void _calculateTextDimension()
        {
            _cachedTextWidths.Clear();
            _cachedTextWidths.Add(0);

            if (_text == null)
            {
                _cachedTextHeight = 0;
                _textDimensionCacheValid = true;
                return;
            }

            var font = ActualFont;
            var height = font.GetHeight(UIScale);
            foreach (var chr in _text)
            {
                if (chr == '\n')
                {
                    _cachedTextWidths.Add(0);
                    height += font.GetLineHeight(UIScale);
                }
                else
                {
                    var metrics = font.GetCharMetrics(chr, UIScale);
                    if (metrics == null)
                    {
                        continue;
                    }

                    _cachedTextWidths[^1] += metrics.Value.Advance;
                }
            }

            _cachedTextHeight = height;
            _textDimensionCacheValid = true;
        }

        protected override void StylePropertiesChanged()
        {
            _textDimensionCacheValid = false;

            base.StylePropertiesChanged();
        }
    }
}
