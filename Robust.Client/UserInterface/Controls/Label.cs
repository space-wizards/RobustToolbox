using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
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
    [Virtual]
    public class Label : SelectableTextControl
    {
        public const string StylePropertyFontColor = "font-color";
        public const string StylePropertyFont = "font";
        public const string StylePropertyAlignMode = "alignMode";

        private int _cachedTextHeight;
        private readonly List<int> _cachedTextWidths = new();
        private bool _textDimensionCacheValid;
        private string? _text;
        private ReadOnlyMemory<char> _textMemory;
        private bool _clipText;
        private AlignMode _align;
        private Font? _fontOverride;
        private int _selectionLower;
        private int _selectionUpper;

        public Label()
        {
            VerticalAlignment = VAlignment.Center;
        }

        /// <summary>
        ///     The text to display.
        /// </summary>
        /// <remarks>
        /// Replaces <see cref="TextMemory"/> when set.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="TextMemory"/> was set directly and there is no backing string instance to fetch.
        /// </exception>
        [ViewVariables]
        [Animatable]
        public string? Text
        {
            get => _text ?? (_textMemory.Length > 0 ? throw new InvalidOperationException("Label uses TextMemory, cannot fetch string text.") : null);
            set
            {
                _text = value;
                _textMemory = value.AsMemory();
                _textDimensionCacheValid = false;
                ClearSelection();
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// The text to display, set as a read-only memory.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Note that updating the backing memory while the control is using it can result in incorrect display due to caching of measure information and similar.
        /// If you modify the backing storage, re-assign the property to invalidate these.
        /// </para>
        /// <para>
        /// Sets <see cref="Text"/> to throw an exception if read, as there is no backing string to retrieve.
        /// </para>
        /// </remarks>
        public ReadOnlyMemory<char> TextMemory
        {
            get => _textMemory;
            set
            {
                _text = null;
                _textMemory = value;
                _textDimensionCacheValid = false;
                ClearSelection();
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

        public Font? FontOverride
        {
            get => _fontOverride;
            set
            {
                _fontOverride = value;
                _textDimensionCacheValid = false;
                InvalidateMeasure();
            }
        }

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
            if (_textMemory.Length == 0)
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

                return new Vector2(hOffset, font.GetAscent(UIScale) + font.GetLineHeight(UIScale) * newlines + vOffset);
            }

            var baseLine = CalcBaseline();
            DrawSelectionIfNeeded(handle);

            foreach (var rune in _textMemory.Span.EnumerateRunes())
            {
                if (rune == new Rune('\n'))
                {
                    newlines += 1;
                    baseLine = CalcBaseline();
                }

                var advance = font.DrawChar(handle, rune, baseLine, UIScale, actualFontColor);
                baseLine += new Vector2(advance, 0);
            }
        }

        protected override ReadOnlySpan<char> GetTextSpan()
        {
            return _textMemory.Span;
        }

        protected override int GetIndexAtPosition(Vector2 relativePosition)
        {
            return GetIndexAtPositionInternal(relativePosition);
        }

        protected override void DrawSelectionRange(DrawingHandleScreen handle, int selectionLower, int selectionUpper)
        {
            if (selectionUpper <= selectionLower)
                return;

            _selectionLower = selectionLower;
            _selectionUpper = selectionUpper;
            DrawSelection(handle, ActualFont);
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
                return new Vector2(0, _cachedTextHeight / UIScale);
            }

            var totalWidth = 0;
            foreach (var width in _cachedTextWidths)
            {
                totalWidth = Math.Max(totalWidth, width);
            }

            return new Vector2(totalWidth / UIScale, _cachedTextHeight / UIScale);
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

            if (_textMemory.Length == 0)
            {
                _cachedTextHeight = 0;
                _textDimensionCacheValid = true;
                return;
            }

            var font = ActualFont;
            var height = font.GetHeight(UIScale);
            foreach (var rune in _textMemory.Span.EnumerateRunes())
            {
                if (rune == new Rune('\n'))
                {
                    _cachedTextWidths.Add(0);
                    height += font.GetLineHeight(UIScale);
                }
                else
                {
                    var metrics = font.GetCharMetrics(rune, UIScale);
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

        /// <summary>
        ///     Maps a control-relative position to a UTF-16 index within the label text.
        /// </summary>
        private int GetIndexAtPositionInternal(Vector2 relativePosition)
        {
            if (_textMemory.Length == 0)
                return 0;

            if (!_textDimensionCacheValid)
            {
                _calculateTextDimension();
                DebugTools.Assert(_textDimensionCacheValid);
            }

            var font = ActualFont;
            var lineCount = _cachedTextWidths.Count;
            var lineHeight = font.GetLineHeight(UIScale);

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

            var yPx = relativePosition.Y * UIScale;
            var line = (int) Math.Floor((yPx - vOffset) / lineHeight);
            line = MathHelper.Clamp(line, 0, lineCount - 1);

            var lineWidth = _cachedTextWidths[line];
            int hOffset;
            switch (Align)
            {
                case AlignMode.Left:
                    hOffset = 0;
                    break;
                case AlignMode.Center:
                case AlignMode.Fill:
                    hOffset = (PixelSize.X - lineWidth) / 2;
                    break;
                case AlignMode.Right:
                    hOffset = PixelSize.X - lineWidth;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var xPx = relativePosition.X * UIScale;
            var lineStartIndex = GetLineStartIndex(line);
            var lineEndIndex = GetLineEndIndex(line);

            if (xPx <= hOffset)
                return lineStartIndex;

            if (xPx >= hOffset + lineWidth)
                return lineEndIndex;

            var index = lineStartIndex;
            var chrPosX = hOffset;
            var lastChrPosX = hOffset;

            var currentLine = 0;
            foreach (var rune in _textMemory.Span.EnumerateRunes())
            {
                if (rune == new Rune('\n'))
                {
                    if (currentLine == line)
                        break;

                    currentLine++;
                    continue;
                }

                if (currentLine != line)
                {
                    index += rune.Utf16SequenceLength;
                    continue;
                }

                if (!font.TryGetCharMetrics(rune, UIScale, out var metrics))
                {
                    index += rune.Utf16SequenceLength;
                    continue;
                }

                if (chrPosX > xPx)
                    break;

                lastChrPosX = chrPosX;
                chrPosX += metrics.Advance;
                index += rune.Utf16SequenceLength;

                if (chrPosX > hOffset + lineWidth)
                    break;
            }

            var distanceRight = chrPosX - xPx;
            var distanceLeft = xPx - lastChrPosX;
            if (index > lineStartIndex && distanceRight > distanceLeft)
            {
                index -= 1;
                if (index > 0 && index < _textMemory.Length && char.IsLowSurrogate(_textMemory.Span[index]))
                    index -= 1;
            }

            return index;
        }

        /// <summary>
        ///     Returns the UTF-16 index of the first character on the given line.
        /// </summary>
        private int GetLineStartIndex(int targetLine)
        {
            var line = 0;
            var index = 0;
            foreach (var rune in _textMemory.Span.EnumerateRunes())
            {
                if (line == targetLine)
                    break;

                index += rune.Utf16SequenceLength;
                if (rune == new Rune('\n'))
                    line++;
            }

            return index;
        }

        /// <summary>
        ///     Returns the UTF-16 index immediately after the last character on the given line.
        /// </summary>
        private int GetLineEndIndex(int targetLine)
        {
            var line = 0;
            var index = 0;
            foreach (var rune in _textMemory.Span.EnumerateRunes())
            {
                if (rune == new Rune('\n'))
                {
                    if (line == targetLine)
                        break;

                    line++;
                }
                else if (line == targetLine)
                {
                    index += rune.Utf16SequenceLength;
                }
                else
                {
                    index += rune.Utf16SequenceLength;
                }
            }

            return index;
        }

        /// <summary>
        ///     Draws selection rectangles for the active UTF-16 index range.
        /// </summary>
        private void DrawSelection(DrawingHandleScreen handle, Font font)
        {
            var lower = _selectionLower;
            var upper = _selectionUpper;
            if (upper <= lower)
                return;

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

            var color = StylePropertyDefault(StylePropertySelectionColor, Color.CornflowerBlue.WithAlpha(0.25f));

            var lineHeight = font.GetLineHeight(UIScale);
            var line = 0;
            var textIndex = 0;

            var lineWidth = _cachedTextWidths[line];
            var lineStartX = GetLineStartX(line, lineWidth);
            var lineTop = vOffset + lineHeight * line;
            var lineBottom = lineTop + lineHeight;

            var tracker = new TextSelectionLineTracker(lower, upper);
            tracker.BeginLine(0, lineStartX, lineTop, lineBottom);

            var x = lineStartX;

            foreach (var rune in _textMemory.Span.EnumerateRunes())
            {
                if (rune == new Rune('\n'))
                {
                    tracker.FinishLine(handle, color, textIndex, lineStartX + lineWidth);

                    line++;
                    lineWidth = _cachedTextWidths[Math.Min(line, _cachedTextWidths.Count - 1)];
                    lineStartX = GetLineStartX(line, lineWidth);
                    lineTop = vOffset + lineHeight * line;
                    lineBottom = lineTop + lineHeight;
                    textIndex += rune.Utf16SequenceLength;
                    x = lineStartX;
                    tracker.BeginLine(textIndex, lineStartX, lineTop, lineBottom);
                    continue;
                }

                tracker.UpdateForIndex(textIndex, x);

                if (font.TryGetCharMetrics(rune, UIScale, out var metrics))
                    x += metrics.Advance;

                textIndex += rune.Utf16SequenceLength;
                tracker.UpdateForIndex(textIndex, x);
            }

            tracker.FinishLine(handle, color, textIndex, x);
        }

        /// <summary>
        ///     Computes the starting X offset for the given line based on alignment.
        /// </summary>
        private float GetLineStartX(int line, int lineWidth)
        {
            return Align switch
            {
                AlignMode.Left => 0,
                AlignMode.Center => (PixelSize.X - lineWidth) / 2f,
                AlignMode.Fill => (PixelSize.X - lineWidth) / 2f,
                AlignMode.Right => PixelSize.X - lineWidth,
                _ => 0
            };
        }
    }
}
