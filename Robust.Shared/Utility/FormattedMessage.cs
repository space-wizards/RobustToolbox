using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Robust.Shared.Utility;

/// <summary>
///     Represents a formatted message in the form of a list of "tags".
///     Does not do any concrete formatting, simply useful as an API surface.
/// </summary>
[PublicAPI]
[Serializable, NetSerializable]
public sealed partial class FormattedMessage
{
    public IReadOnlyList<MarkupNode> Nodes => _nodes.AsReadOnly();
    public bool IsEmpty => _nodes.Count > 0;

    private readonly List<MarkupNode> _nodes;

    private readonly Stack<MarkupNode> _openNodeStack = new();

    public FormattedMessage()
    {
        _nodes = new List<MarkupNode>();
    }

    public FormattedMessage(int capacity)
    {
        _nodes = new List<MarkupNode>(capacity);
    }

    /// <summary>
    ///     Create a new <c>FormattedMessage</c> by copying another one.
    /// </summary>
    /// <param name="toCopy">The message to copy.</param>
    public FormattedMessage(FormattedMessage toCopy)
    {
        _nodes = Extensions.ShallowClone<MarkupNode>(toCopy._nodes);
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

    public void AddText(string text)
    {
        PushTag(new MarkupNode(text));
    }

    public void PushColor(Color color)
    {
        PushTag(new MarkupNode("color", new MarkupParameter(color), null));
    }

    public void PushNewline()
    {
        AddText("\n");
    }

    public void PushTag(MarkupNode markupNode, bool selfClosing = false)
    {
        _nodes.Add(markupNode);

        if (markupNode.Name == null)
            return;

        if (selfClosing)
        {
            _nodes.Add(new MarkupNode(markupNode.Name, null, null, true));
            return;
        }

        _openNodeStack.Push(markupNode);
    }

    public void Pop()
    {
        if (!_openNodeStack.TryPop(out var node))
            return;

        _nodes.Add(new MarkupNode(node.Name, null, null, true));
    }

    public void AddMessage(FormattedMessage other)
    {
        _nodes.AddRange(other._nodes);
    }

    public void Clear()
    {
        _nodes.Clear();
    }

    public FormattedMessageRuneEnumerator EnumerateRunes()
    {
        return new FormattedMessageRuneEnumerator(this);
    }

    /// <returns>The string without markup tags.</returns>
    public override string ToString()
    {
        var builder = new StringBuilder();

        foreach (var node in _nodes)
        {
            if (node.Name == null)
                builder.Append(node.Value.StringValue);
        }

        return builder.ToString();
    }

    /// <returns>The string without filtering out markup tags.</returns>
    public string ToMarkup()
    {
        return string.Join("", _nodes);
    }

    public struct FormattedMessageRuneEnumerator : IEnumerable<Rune>, IEnumerator<Rune>
    {
        private readonly FormattedMessage _msg;
        private IEnumerator<MarkupNode> _tagEnumerator;
        private StringRuneEnumerator _runeEnumerator;

        internal FormattedMessageRuneEnumerator(FormattedMessage msg)
        {
            _msg = msg;
            _tagEnumerator = msg.Nodes.GetEnumerator();
            // Rune enumerator will immediately give false on first iteration so I dont' need to special case anything.
            _runeEnumerator = "".EnumerateRunes();
        }

        public IEnumerator<Rune> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool MoveNext()
        {
            while (!_runeEnumerator.MoveNext())
            {
                MarkupNode text;
                while (true)
                {
                    var result = _tagEnumerator.MoveNext();
                    if (!result)
                        return false;

                    if (_tagEnumerator.Current is not { Name: null, Value.StringValue: not null } nextText)
                        continue;

                    text = nextText;
                    break;
                }

                _runeEnumerator = text.Value.StringValue!.EnumerateRunes();
            }

            return true;
        }

        public void Reset()
        {
            _tagEnumerator = _msg.Nodes.GetEnumerator();
            _runeEnumerator = "".EnumerateRunes();
        }

        public Rune Current => _runeEnumerator.Current;

        object IEnumerator.Current => Current;

        void IDisposable.Dispose()
        {
        }
    }
}
