using System;
using System.Collections.Generic;
using SS14.Shared.Maths;
using SS14.Shared.Serialization;

namespace SS14.Client.Utility
{
    /// <summary>
    ///     Represents a formatted message in the form of a list of "tags".
    ///     Does not do any concrete formatting, simply useful as an API surface.
    /// </summary>
    public class FormattedMessage
    {
        public IEnumerable<Tag> Tags => _tags;
        readonly List<Tag> _tags;

        public FormattedMessage()
        {
            _tags = new List<Tag>();
        }

        public FormattedMessage(int capacity)
        {
            _tags = new List<Tag>(capacity);
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


        public abstract class Tag
        { }

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
    }
}
