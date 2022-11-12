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
    ///     Represents a formatted message in the form of a list of "tags".
    ///     Does not do any concrete formatting, simply useful as an API surface.
    /// </summary>
    [PublicAPI]
    [Serializable, NetSerializable]
    public sealed partial class FormattedMessage
    {
        public TagList Tags => new(_tags);
        private readonly List<Tag> _tags;

        public bool IsEmpty => _tags.Count == 0;

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

        public FormattedMessageRuneEnumerator EnumerateRunes()
        {
            return new FormattedMessageRuneEnumerator(this);
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

    public struct FormattedMessageRuneEnumerator : IEnumerable<Rune>, IEnumerator<Rune>
    {
        private readonly FormattedMessage _msg;
        private List<FormattedMessage.Tag>.Enumerator _tagEnumerator;
        private StringRuneEnumerator _runeEnumerator;

        internal FormattedMessageRuneEnumerator(FormattedMessage msg)
        {
            _msg = msg;
            _tagEnumerator = msg.Tags.GetEnumerator();
            // Rune enumerator will immediately give false on first iteration so I dont' need to special case anything.
            _runeEnumerator = "".EnumerateRunes();
        }

        public IEnumerator<Rune> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool MoveNext()
        {
            while (!_runeEnumerator.MoveNext())
            {
                FormattedMessage.TagText text;
                while (true)
                {
                    var result = _tagEnumerator.MoveNext();
                    if (!result)
                        return false;

                    if (_tagEnumerator.Current is not FormattedMessage.TagText { Text.Length: > 0 } nextText)
                        continue;

                    text = nextText;
                    break;
                }

                _runeEnumerator = text.Text.EnumerateRunes();
            }

            return true;
        }

        public void Reset()
        {
            _tagEnumerator = _msg.Tags.GetEnumerator();
            _runeEnumerator = "".EnumerateRunes();
        }

        public Rune Current => _runeEnumerator.Current;

        object IEnumerator.Current => Current;

        void IDisposable.Dispose()
        {
        }
    }
}
