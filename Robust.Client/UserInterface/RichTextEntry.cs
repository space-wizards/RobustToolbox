using System.Collections.Immutable;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface
{
    /// <summary>
    ///     Used by <see cref="OutputPanel"/> and <see cref="RichTextLabel"/> to handle rich text layout.
    /// </summary>
    internal struct RichTextEntry
    {
        public readonly FormattedMessage Message;

        /// <summary>
        ///     The vertical size of this entry, in pixels.
        /// </summary>
        public int Height;

        /// <summary>
        ///     The horizontal size of this entry, in pixels.
        /// </summary>
        public int Width;

        public RichTextEntry(FormattedMessage message)
        {
            Message = message;
            Height = 0;
            Width = 0;
        }

        // Last maxSizeX, used to detect resizing.
        private int _lmsx = 0;
        // Layout data, which needs to be refreshed when resized.
        private ImmutableArray<TextLayout.Offset> _ld = default;

        /// <summary>
        ///     Recalculate line dimensions and where it has line breaks for word wrapping.
        /// </summary>
        /// <param name="font">The font being used for display.</param>
        /// <param name="maxSizeX">The maximum horizontal size of the container of this entry.</param>
        /// <param name="uiScale"></param>
        public void Update(IFontLibrary font, float maxSizeX, float uiScale)
        {
            if ((int) maxSizeX != _lmsx)
            {
                _ld = TextLayout.Layout(Message, (int) maxSizeX, font, scale: uiScale);
                Height = 0;
                Width = 0;
                foreach (var w in _ld)
                {
                    if (w.x + w.w > Width) Width = w.x + w.w;
                    if (w.y + w.h > Height) Height = w.y + w.h;
                }
            }
        }

        public void Draw(
            DrawingHandleScreen handle,
            IFontLibrary font,
            UIBox2 drawBox,
            float verticalOffset,
            float uiScale)
        {
            var flib = font.StartFont();
            foreach (var wd in _ld)
            {
                var s = Message.Sections[wd.section];
                var baseLine = drawBox.TopLeft + new Vector2((float) wd.x, (float) wd.y);
                foreach (var rune in s
                        .Content[wd.charOffs..(wd.charOffs+wd.length)]
                        .EnumerateRunes())
                {
                    baseLine.X += flib.Current.DrawChar(
                            handle,
                            rune,
                            baseLine,
                            uiScale,
                            new Color { // Why Color.FromArgb isn't a thing is beyond me.
                                A=(float) ((s.Color & 0xFF_00_00_00) >> 24),
                                R=(float) ((s.Color & 0x00_FF_00_00) >> 16),
                                G=(float) ((s.Color & 0x00_00_FF_00) >> 8),
                                B=(float) (s.Color & 0x00_00_00_FF)
                            }
                    );
                }
            }
        }
    }
}
