using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Robust.Shared.Utility
{
    public interface ISectionable
    {
        Section this[int i] { get; }
        int Length { get; }
    }

    [Serializable, NetSerializable]
    public record struct Section
    {
        public FontStyle Style = default;
        public FontSize Size = default;
        public TextAlign Alignment = default;
        public int Color = default;
        public MetaFlags Meta = default;
        public string Content = string.Empty;
    }

    [Flags]
    public enum MetaFlags : byte
    {
        Default   = 0,
        Localized = 1,
        // All other values are reserved.
    }

    [Flags]
    public enum FontStyle : byte
    {
        // Single-font styles
        Normal     = 0b0000_0000,
        Bold       = 0b0000_0001,
        Italic     = 0b0000_0010,
        Monospace  = 0b0000_0100,
        BoldItalic = Bold | Italic,

        // Escape value
        Special    = 0b1000_0000,

        // The lower four bits are available for styles to specify.
        Standard   = 0b0100_0000 | Special,

        // All values not otherwise specified are reserved.
    }

    [Flags]
    public enum FontSize : ushort
    {
        // Format (Standard): 0bSNNN_NNNN_NNNN_NNNN
        // S: Special flag.
        // N (where S == 0): Font size. Unsigned.
        // N (where S == 1): Special operation; see below.

        // Flag to indicate the TagFontSize is "special".
        // All values not specified are reserved.
        // General Format: 0b1PPP_AAAA_AAAA_AAAA
        // P: Operation.
        // A: Arguments.
        Special  = 0b1000_0000_0000_0000,

        // RELative Plus.
        // Format: 0b1100_NNNN_NNNN_NNNN
        // N: Addend to the previous font size. Unsigned.
        RelPlus  = 0b0100_0000_0000_0000 | Special,

        // RELative Minus.
        // Format: 0b1010_NNNN_NNNN_NNNN
        // N: Subtrahend to the previous font size. Unsigned.
        RelMinus = 0b0010_0000_0000_0000 | Special,

        // Selects a font size from the stylesheet.
        // Format: 0b1110_NNNN_NNNN_NNNN
        // N: The identifier of the preset font size.
        Standard = 0b0110_0000_0000_0000 | Special,
    }

    public enum TextAlign : byte
    {
        // Format: 0bHHHH_VVVV
        // H: Horizontal alignment
        // V: Vertical alignment.
        // All values not specified are reserved.

        // This seems dumb to point out, but ok
        Default     = Baseline | Left,

        // Vertical alignment
        Baseline    = 0x00,
        Top         = 0x01,
        Bottom      = 0x02,
        Superscript = 0x03,
        Subscript   = 0x04,

        // Horizontal alignment
        Left        = 0x00,
        Right       = 0x10,
        Center      = 0x20,
        Justify     = 0x30,
    }

    public static partial class Extensions
    {
        public static TextAlign Vertical (this TextAlign value) => (TextAlign)((byte) value & 0x0F);
        public static TextAlign Horizontal (this TextAlign value) => (TextAlign)((byte) value & 0xF0);
    }

    [PublicAPI]
    [Serializable, NetSerializable]
    public sealed record FormattedMessage(Section[] Sections) : ISectionable
    {
        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var i in Sections)
                sb.Append(i.Content);

            return sb.ToString();
        }

        // I don't wanna fix the serializer yet.
        public string ToMarkup()
        {
            #warning FormattedMessage.ToMarkup is still lossy.
            var sb = new StringBuilder();
            foreach (var i in Sections)
            {
                if (i.Content.Length == 0)
                    continue;

                if (i.Color != default)
                    sb.AppendFormat("[color=#{0:X8}]",
                            // Bit twiddling to swap AARRGGBB to RRGGBBAA
                            ((i.Color << 8) & 0xFF_FF_FF_00) | // Drop alpha from the front
                            ((i.Color & 0xFF_00_00_00) >> 24)  // Shuffle it to the back
                            );

                sb.Append(i.Content);

                if (i.Color != default)
                    sb.Append("[/color]");
            }

            return sb.ToString();
        }

        public static readonly FormattedMessage Empty = new FormattedMessage(Array.Empty<Section>());

        public Section this[int i] { get => Sections[i]; }
        public int Length { get => Sections.Length; }

        // are you a construction worker?
        // cuz you buildin
        [Obsolete("Construct FormattedMessage Sections manually.")]
        public class Builder
        {
            // _dirty signals that _sb has content that needs flushing to _work
            private bool _dirty = false;

            // We fake a stack by keeping an index in to the work list.
            // Since each Section contains all its styling info, we can "pop" the stack by
            // using the (unchanged) Section before it.
            private int _idx = 0;
            private StringBuilder _sb = new();

            // _work starts out with a dummy item because otherwise we break the assumption that
            // _idx will always refer to *something* in _work.
            private List<Section> _work = new() {
                new Section()
            };

            public static Builder FromFormattedText(FormattedMessage orig) => new ()
            {
                // Again, we always need at least one _work item, so if the FormattedMessage
                // is empty, we'll forge one.
                _idx = orig.Sections.Length < 0 ? orig.Sections.Length - 1 : 0,
                _work = new List<Section>(
                    orig.Sections.Length == 0 ?
                        new [] { new Section() }
                        : orig.Sections
                ),
            };

            // hmm what could this do
            public void Clear()
            {
                _dirty = false;
                _idx = 0;
                _work = new() {
                    new Section()
                };
                _sb = _sb.Clear();
            }

            // Since we don't change any styling, we don't need to add a full Section.
            // In these cases, we add it to the StringBuilder, and wait until styling IS changed,
            // or we Render().
            public void AddText(string text)
            {
                _dirty = true;
                _sb.Append(text);
            }

            // PushColor changes the styling, so we need to submit any text we had waiting, then
            // add a new empty Section with the new color.
            public void PushColor(Color color)
            {
                flushWork();

                var last = _work[_idx];
                last.Color = color.ToArgb();
                _work.Add(last);
                _idx = _work.Count - 1;
            }

            // These next two are probably wildly bugged, since they'll include the other sections
            // wholesale, and the entire fake-stack facade breaks down, since there's no way for the
            // new stuff to inherit the previous style, and we don't know what parts of the style are
            // actually set, and what parts are just default values.

            // TODO: move _idx?
            public void AddMessage(FormattedMessage other) =>
                _work.AddRange(other.Sections);

            // TODO: See above
            public void AddMessage(FormattedMessage.Builder other) =>
                _work.AddRange(other._work);

            // I wish I understood why this was needed...
            // Did people not know you could AddText("\n")?
            public void PushNewline()
            {
                _dirty = true;
                _sb.Append('\n');
            }

            // Flush any text we've got for the current style,
            // then roll back to the style before this one.
            public void Pop()
            {
                flushWork();
                // Go back one (or stay at the start)
                _idx = (_idx > 0) ? (_idx - 1) : 0;
            }

            public void flushWork()
            {
                // Nothing changed? Great.
                if (!_dirty)
                    return;

                // Get the last tag (for the style)...
                var last = _work[_idx];
                // ...and set the content to the current buffer
                last.Content = _sb.ToString();
                _work.Add(last);

                // Clean up
                _sb = _sb.Clear();
                _dirty = false;
            }

            public FormattedMessage Build()
            {
                flushWork();
                return new FormattedMessage(_work
                        .GetRange(1, _work.Count - 1)      // Drop the placeholder
                        .Where(e => e.Content.Length != 0) // and any blanks (which can happen from pushing colors and such)
                        .ToArray());
            }
        }
    }
}
