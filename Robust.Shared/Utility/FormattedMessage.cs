using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using JetBrains.Annotations;
using Nett.Parser;
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
    /// <summary>
    /// The list of nodes the formatted message is made out of
    /// </summary>
    public IReadOnlyList<MarkupNode> Nodes => _nodes.AsReadOnly();

    /// <summary>
    /// true if the formatted message doesn't contain any nodes
    /// </summary>
    public bool IsEmpty => _nodes.Count == 0;

    private readonly List<MarkupNode> _nodes;

    /// <summary>
    /// Used for inserting the correct closing node when calling <see cref="Pop"/>
    /// </summary>
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
        _nodes = toCopy._nodes.ShallowClone();
    }

    private FormattedMessage(List<MarkupNode> nodes)
    {
        _nodes = nodes;
    }

    /// <summary>
    /// Attempt to create a new formatted message from some markup text. Returns an error if it fails.
    /// </summary>
    public static bool TryFromMarkup(string markup, [NotNullWhen(true)] out FormattedMessage? msg, [NotNullWhen(false)] out string? error)
    {
        if (!TryParse(markup, out var nodes, out error))
        {
            msg = null;
            return false;
        }

        msg = new FormattedMessage(nodes);
        return true;
    }

    /// <summary>
    /// Attempt to create a new formatted message from some markup text.
    /// </summary>
    public static bool TryFromMarkup(string markup, [NotNullWhen(true)] out FormattedMessage? msg)
        => TryFromMarkup(markup, out msg, out _);

    /// <summary>
    /// Attempt to create a new formatted message from some markup text. Throws if the markup is invalid.
    /// </summary>
    /// <exception cref="ParseException">Thrown when an error occurs while trying to parse the markup.</exception>
    public static FormattedMessage FromMarkupOrThrow(string markup)
    {
        var msg = new FormattedMessage();
        msg.AddMarkupOrThrow(markup);
        return msg;
    }

    [Obsolete("Use FromMarkupOrThrow or TryFromMarkup")]
    public static FormattedMessage FromMarkup(string markup) => FromMarkupOrThrow(markup);

    public static FormattedMessage FromUnformatted(string text)
    {
        var msg = new FormattedMessage();
        msg.AddText(text);
        return msg;
    }

    /// <summary>
    /// Variant of <see cref="TryFromMarkup(string,out Robust.Shared.Utility.FormattedMessage?,out string?)"/> that
    /// attempts to fall back to using the permissive parser that interprets invalid markup tags as normal text.
    /// This may still throw if the permissive parser fails.
    /// </summary>
    /// <exception cref="ParseException">Thrown when an error occurs while trying to parse the markup.</exception>
    public static FormattedMessage FromMarkupPermissive(string markup, out string? error)
    {
        var msg = new FormattedMessage();
        msg.AddMarkupPermissive(markup, out error);
        return msg;
    }

    /// <inheritdoc cref="FromMarkupPermissive(string,out string?)"/>
    public static FormattedMessage FromMarkupPermissive(string markup) => FromMarkupPermissive(markup, out _);

    /// <summary>
    ///     Escape a string of text to be able to be formatted into markup.
    /// </summary>
    public static string EscapeText(string text)
    {
        return text.Replace("\\", "\\\\").Replace("[", "\\[");
    }

    /// <summary>
    ///     Remove all markup, leaving only the basic text content behind. Throws if it fails to parse the markup tags.
    /// </summary>
    /// <exception cref="ParseException">Thrown when an error occurs while trying to parse the markup.</exception>
    public static string RemoveMarkupOrThrow(string markup)
    {
        return FromMarkupOrThrow(markup).ToString();
    }

    /// <summary>
    /// Attempts to remove all valid markup tags, leaving only the basic text content behind.
    /// If this markup contains invalid tags that cannot be parsed, they will not be removed and will instead be trated
    /// as normal text. Hence the output should probably only be parsed using try-parse the permissive parser.
    /// </summary>
    /// <exception cref="ParseException">Thrown when an error occurs while trying to fall back to the permissive parser.</exception>
    public static string RemoveMarkupPermissive(string markup)
    {
        return FromMarkupPermissive(markup).ToString();
    }

    [Obsolete("Use RemoveMarkupOrThrow or RemoveMarkupPermissive")]
    public static string RemoveMarkup(string markup) => RemoveMarkupOrThrow(markup);

    /// <summary>
    /// Adds a text node.
    /// This node doesn't need to be closed with <see cref="Pop"/>.
    /// </summary>
    /// <param name="text">The text to add</param>
    public void AddText(string text)
    {
        PushTag(new MarkupNode(text));
    }

    /// <summary>
    /// Adds an open color node. It needs to later be closed by calling <see cref="Pop"/>
    /// </summary>
    /// <param name="color">The color of the node to add</param>
    public void PushColor(Color color)
    {
        PushTag(new MarkupNode("color", new MarkupParameter(color), null));
    }

    /// <summary>
    /// Adds a newline as a text node.
    /// This node doesn't need to be closed with <see cref="Pop"/>.
    /// </summary>
    public void PushNewline()
    {
        AddText("\n");
    }

    /// <summary>
    /// Adds a new open node to the formatted message.
    /// The method for inserting closed nodes: <see cref="Pop"/>. It needs to be
    /// called once for each inserted open node that isn't self closing.
    /// </summary>
    /// <param name="markupNode">The node to add</param>
    /// <param name="selfClosing">Whether the node is self closing or not.
    /// Self closing nodes automatically insert a closing node after the open one</param>
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

    /// <summary>
    /// Closes the last added node that wasn't self closing
    /// </summary>
    public void Pop()
    {
        if (!_openNodeStack.TryPop(out var node))
            return;

        _nodes.Add(new MarkupNode(node.Name, null, null, true));
    }

    /// <summary>
    /// Adds a formatted message to this one.
    /// </summary>
    /// <param name="other">The formatted message to be added</param>
    public void AddMessage(FormattedMessage other)
    {
        _nodes.AddRange(other._nodes);
    }

    /// <summary>
    /// Clears the formatted message
    /// </summary>
    public void Clear()
    {
        _nodes.Clear();
    }

    /// <summary>
    /// Returns an enumerator that enumerates every rune for each text node contained in this formatted text instance.
    /// </summary>
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
