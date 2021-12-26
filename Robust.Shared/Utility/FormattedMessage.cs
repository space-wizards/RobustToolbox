using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Robust.Shared.Utility
{
    /// <summary>
    /// A thin wrapper to allow composing behaviors on top of <see cref="NewFormattedMessage"/>s
    /// </summary>
    public interface ISectionable
    {
        Section this[int i] { get; }
        int Length { get; }
    }

    /// <summary>
    /// A container for a block of text with identical rendering properties.
    /// </summary>
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

    /// <summary>
    /// Flags that can be used to alter the behavior of <see cref="Section"/>s.
    /// </summary>
    [Flags]
    public enum MetaFlags : byte
    {
        Default   = 0,
        Localized = 1,
        // All other values are reserved.
    }

    /// <summary>
    /// Flags that identify a specific variation of a font.
    /// </summary>
    /// <remarks>
    /// Can also be used to specify a global font by defining a <see cref="FontStyle.Standard"/> style in the <see cref="Robust.Client.Graphics.IFontLibrary"/>.
    /// All values not specified are reserved.
    /// </remarks>
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

        /// <summary>
        /// Causes renderers to re-use a font from a library.
        /// </summary>
        /// <remarks>
        /// The lower four bits are the identifier of the specific special font.
        /// </remarks>
        Standard   = 0b0100_0000 | Special,
    }

    /// <summary>
    /// A number that selects a font size (in points), a change in font size, or a "standard" font size.
    ///
    /// Format (<see cref="FontSize.Special"/>): <c>0bSNNN_NNNN_NNNN_NNNN</c>
    /// <list type="table">
    /// <item><term><c>S</c></term><description>Special flag.</description></item>
    /// <item><term><c>N</c> (where <c>S == 0</c>)</term> <description>Font size. Unsigned.</description></item>
    /// <item><term><c>N</c> (where <c>S == 1</c>)</term> <description>Special operation; see below.</description></item>
    /// </list>
    /// </summary>
    [Flags]
    public enum FontSize : ushort
    {
        /// <summary>
        /// Flag to indicate the <see cref="FontSize"/> is "special".
        ///
        /// General Format: <c>0b1PPP_AAAA_AAAA_AAAA</c>
        /// <list type="table">
        /// <item><term><c>P</c></term> <description>Operation.</description></item>
        /// <item><term><c>A</c></term> <description>Arguments.</description></item>
        /// </list>
        ///
        /// All values not specified are reserved.
        /// </summary>
        Special  = 0b1000_0000_0000_0000,

        /// <summary>
        /// RELative Plus.
        /// 
        /// Format: <c>0b1100_NNNN_NNNN_NNNN</c>
        /// <list type="table">
        /// <item><term><c>N</c></term> <description>Addend to the previous font size. Unsigned.</description></item>
        /// </list>
        /// </summary>
        RelPlus  = 0b0100_0000_0000_0000 | Special,

        /// <summary>
        /// RELative Minus.
        ///
        /// Format: <c>0b1010_NNNN_NNNN_NNNN</c>
        /// <list type="table">
        /// <item><term><c>N</c></term> <description>Subtrahend to the previous font size. Unsigned.</description></item>
        /// </list>
        /// </summary>
        RelMinus = 0b0010_0000_0000_0000 | Special,

        /// <summary>
        /// Selects a font size from the stylesheet.
        ///
        /// Format: <c>0b1110_NNNN_NNNN_NNNN</c>
        /// <list type="table">
        /// <item><term><c>N</c></term> <description>Subtrahend to the previous font size. Unsigned.</description></item>
        /// </list>
        /// </summary>
        Standard = 0b0110_0000_0000_0000 | Special,
    }

    /// <summary>
    /// Two nibbles (aka "a byte") that select a <see cref="Section"/>'s text alignment.
    ///
    /// Format: <c>0bHHHH_VVVV</c>
    /// <list type="table">
    /// <item><term><c>H</c></term> <description>Horizontal Alignment</description></item>
    /// <item><term><c>V</c></term> <description>Vertical Alignment</description></item>
    /// </list>
    ///
    /// All values not specified are reserved.
    /// </summary>
    public enum TextAlign : byte
    {

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
    [Obsolete("Use NewFormattedMessage instead.")]
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
