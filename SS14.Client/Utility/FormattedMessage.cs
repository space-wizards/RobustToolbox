using System.Collections;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using SS14.Shared.Maths;
using SS14.Shared.Utility;

namespace SS14.Client.Utility
{
    /// <summary>
    ///     Represents a formatted message in the form of a list of "tags".
    ///     Does not do any concrete formatting, simply useful as an API surface.
    /// </summary>
    [PublicAPI]
    public sealed class FormattedMessage
    {
        public TagList Tags => new TagList(_tags);
        private readonly List<Tag> _tags;

        public FormattedMessage()
        {
            _tags = new List<Tag>();
        }

        public FormattedMessage(int capacity)
        {
            _tags = new List<Tag>(capacity);
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

        public void Pop()
        {
            _tags.Add(new TagPop());
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            foreach (var tag in _tags)
            {
                if (!(tag is TagText text))
                {
                    continue;
                }

                builder.Append(text.Text);
            }

            return builder.ToString();
        }

        public abstract class Tag
        {
        }

        public class TagText : Tag
        {
            public readonly string Text;

            public TagText(string text)
            {
                Text = text;
            }
        }

        public class TagColor : Tag
        {
            public readonly Color Color;

            public TagColor(Color color)
            {
                Color = color;
            }
        }

        public class TagPop : Tag
        {
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
