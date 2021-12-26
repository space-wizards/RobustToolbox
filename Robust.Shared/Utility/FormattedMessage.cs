using System;
using System.Collections;
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

    /// <summary>
    ///     Represents a formatted message in the form of a list of "tags".
    ///     Does not do any concrete formatting, simply useful as an API surface.
    /// </summary>
    [PublicAPI]
    [Serializable, NetSerializable]
    public sealed partial class FormattedMessage
    {
        public TagList Tags => new(_tags);
        private readonly List<Tag> _tags;

        public FormattedMessage()
        {
            _tags = new List<Tag>();
        }

        public FormattedMessage(int capacity)
        {
            _tags = new List<Tag>(capacity);
        }

        public static FormattedMessage FromMarkup(string markup)
        {
            var msg = new FormattedMessage();
            msg.AddMarkup(markup);
            return msg;
        }

        public static FormattedMessage FromMarkupPermissive(string markup)
        {
            var msg = new FormattedMessage();
            msg.AddMarkupPermissive(markup);
            return msg;
        }

        /// <summary>
        ///     Escape a string of text to be able to be formatted into markup.
        /// </summary>
        public static string EscapeText(string text)
        {
            return text.Replace("\\", "\\\\").Replace("[", "\\[");
        }

        /// <summary>
        ///     Remove all markup, leaving only the basic text content behind.
        /// </summary>
        public static string RemoveMarkup(string text)
        {
            return FromMarkup(text).ToString();
        }

        /// <summary>
        ///     Create a new <c>FormattedMessage</c> by copying another one.
        /// </summary>
        /// <param name="toCopy">The message to copy.</param>
        public FormattedMessage(FormattedMessage toCopy)
        {
            _tags = toCopy._tags.ShallowClone();
        }

        public void AddText(string text)
        {
            _tags.Add(new TagText(text));
        }

        public void PushColor(Color color)
        {
            _tags.Add(new TagColor(color));
        }

        public void PushNewline()
        {
            AddText("\n");
        }

        public void Pop()
        {
            _tags.Add(new TagPop());
        }

        public void AddMessage(FormattedMessage other)
        {
            _tags.AddRange(other.Tags);
        }

        public void Clear()
        {
            _tags.Clear();
        }

        /// <returns>The string without markup tags.</returns>
        public override string ToString()
        {
            var builder = new StringBuilder();
            foreach (var tag in _tags)
            {
                if (tag is not TagText text)
                {
                    continue;
                }

                builder.Append(text.Text);
            }

            return builder.ToString();
        }

        /// <returns>The string without filtering out markup tags.</returns>
        public string ToMarkup()
        {
            var builder = new StringBuilder();
            foreach (var tag in _tags)
            {
                builder.Append(tag);
            }

            return builder.ToString();
        }

        [Serializable, NetSerializable]
        public abstract record Tag
        {
        }

        [Serializable, NetSerializable]
        public sealed record TagText(string Text) : Tag
        {
            public override string ToString()
            {
                return Text;
            }
        }

        [Serializable, NetSerializable]
        public sealed record TagColor(Color Color) : Tag
        {
            public override string ToString()
            {
                return $"[color={Color.ToHex()}]";
            }
        }

        [Serializable, NetSerializable]
        public sealed record TagPop : Tag
        {
            public static readonly TagPop Instance = new();

            public override string ToString()
            {
                return $"[/color]";
            }
        }

        public readonly struct TagList : IReadOnlyList<Tag>
        {
            private readonly List<Tag> _tags;

            public TagList(List<Tag> tags)
            {
                _tags = tags;
            }

            public List<Tag>.Enumerator GetEnumerator()
            {
                return _tags.GetEnumerator();
            }

            IEnumerator<Tag> IEnumerable<Tag>.GetEnumerator()
            {
                return _tags.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _tags.GetEnumerator();
            }

            public int Count => _tags.Count;

            public Tag this[int index] => _tags[index];
        }
    }
}
