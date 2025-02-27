using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
public sealed partial class FormattedMessage : IReadOnlyList<MarkupNode>
{
    public static FormattedMessage Empty => new();

    /// <summary>
    /// The list of nodes the formatted message is made out of
    /// </summary>
    public IReadOnlyList<MarkupNode> Nodes => _nodes;

    /// <summary>
    /// true if the formatted message doesn't contain any nodes
    /// </summary>
    public bool IsEmpty => _nodes.Count == 0;

    public int Count => _nodes.Count;

    public MarkupNode this[int index] => _nodes[index];

    private readonly List<MarkupNode> _nodes;

    /// <summary>
    /// Used for inserting the correct closing node when calling <see cref="Pop"/>
    /// </summary>
    private Stack<MarkupNode>? _openNodeStack;

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

        _openNodeStack ??= new Stack<MarkupNode>();
        _openNodeStack.Push(markupNode);
    }

    /// <summary>
    /// Closes the last added node that wasn't self closing
    /// </summary>
    public void Pop()
    {
        if (_openNodeStack == null || !_openNodeStack.TryPop(out var node))
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

    # region Helper Methods

    /// <summary>
    /// Helper method that inserts a node at a specific index.
    /// Unless a node is text, an end index must be specified for the closing node.
    /// </summary>
    /// <param name="markupNode">The node to be inserted.</param>
    /// <param name="startIndex">The index of the opening node.</param>
    /// <param name="endIndex">The index of the closing node, if not a text node.</param>
    public void InsertAtIndex(MarkupNode markupNode, int startIndex, int? endIndex = null)
    {
        if (startIndex > _nodes.Count)
            throw new ArgumentOutOfRangeException("startIndex", "startIndex must be less than or equal to the number of nodes.");

        if (markupNode.Name == null)
        {
            _nodes.Insert(startIndex, markupNode);
            return;
        }

        ArgumentNullException.ThrowIfNull(endIndex);
        if (endIndex > _nodes.Count)
            throw new ArgumentOutOfRangeException("endIndex", "endIndex must be less than or equal to the number of nodes.");

        if (startIndex > endIndex.Value)
            throw new ArgumentException("startIndex must be less than or equal to endIndex.");

        _nodes.Insert(startIndex, markupNode);
        _nodes.Insert(endIndex.Value + 1, new MarkupNode(markupNode.Name, null, null, true));
    }

    /// <summary>
    /// Helper method that inserts a node around the message.
    /// </summary>
    /// <param name="markupNode">The node to be inserted; may not be a text node.</param>
    public void InsertAroundMessage(MarkupNode markupNode)
    {
        if (markupNode.Name == null)
        {
            throw new ArgumentException(
                "Argument is supposed to be tag, but was plain text. Cannot wrap message with plain text.",
                nameof(markupNode)
            );
        }

        InsertAtIndex(markupNode, 0, _nodes.Count);
    }

    /// <summary>
    /// Helper method that inserts a node surrounding the first and last text node.
    /// </summary>
    /// <param name="markupNode">The node to be inserted; may not be a text node.</param>
    public void InsertAroundText(MarkupNode markupNode)
    {
        if (markupNode.Name == null)
        {
            throw new ArgumentException(
                "Argument is supposed to be tag, but was plain text. Cannot wrap message with plain text.",
                nameof(markupNode)
            );
        }

        var firstIndex = _nodes.FindIndex(x => x.Name == null);
        var lastIndex = _nodes.FindLastIndex(x => x.Name == null);

        InsertAtIndex(markupNode, firstIndex, lastIndex + 1);
    }

    /// <summary>
    /// Helper method that inserts a node around a string.
    /// </summary>
    /// <param name="markupNode">The node to be inserted; may not be a text node.</param>
    /// <param name="stringText">The string to look for when inserting.</param>
    /// <param name="matchCase">If true, the string case must match exactly.</param>
    public void InsertAroundString(MarkupNode markupNode, string stringText, bool matchCase = true)
    {
        if (markupNode.Name == null)
        {
            throw new ArgumentException(
                "Argument is supposed to be tag, but was plain text. Cannot wrap message with plain text.",
                nameof(markupNode)
            );
        }
        
        for (int i = 0; i < _nodes.Count; i++)
        {
            var node = _nodes[i];

            if (node.Name == null && node.Value.StringValue != null)
            {
                var stringNode = node.Value.StringValue;
                var stringLocation = matchCase ? stringNode.IndexOf(stringText, StringComparison.Ordinal) : stringNode.IndexOf(stringText, StringComparison.OrdinalIgnoreCase);

                if (stringLocation != -1)
                {
                    // Original node needs to be removed, to make room for the new ones.
                    _nodes.RemoveAt(i);

                    // j keeps track of the number of added nodes.
                    var j = 0;

                    var beforeText = stringNode.Substring(0, stringLocation);
                    if (beforeText != "")
                    {
                        InsertAtIndex(new MarkupNode(beforeText), i);
                        j++;
                    }

                    InsertAtIndex(new MarkupNode(stringNode.Substring(stringLocation, stringText.Length)), i + j);
                    InsertAtIndex(markupNode, i + j, i + j + 1);

                    j += 3;

                    var afterText = stringNode.Substring(stringLocation + stringText.Length);
                    if (afterText != "")
                    {
                        InsertAtIndex(new MarkupNode(afterText), i + j);
                    }

                    // We make sure to move the i forwards, searching for additional instances of the string.
                    i += j - 2;
                }
            }
        }
    }

    /// <summary>
    /// Helper function that inserts a node before tags of a specific type.
    /// </summary>
    /// <param name="markupNode">The node to be inserted.</param>
    /// <param name="tagText">The tag to search for.</param>
    public void InsertBeforeTag(MarkupNode markupNode, string tagText)
    {
        var i = _nodes.FindIndex(x => x.Name == tagText && !x.Closing);

        while (i != -1)
        {
            InsertAtIndex(markupNode, i, i);

            if (i + 2 >= _nodes.Count)
                break;

            i = _nodes.FindIndex(i + 2, x => x.Name == tagText && !x.Closing);
        }
    }

    /// <summary>
    /// Helper function that inserts a node after tags of a specific type.
    /// </summary>
    /// <param name="markupNode">The node to be inserted.</param>
    /// <param name="tagText">The tag to search for.</param>
    public void InsertAfterTag(MarkupNode markupNode, string tagText)
    {
        var i = _nodes.FindIndex(x => x.Name == tagText && x.Closing);

        while (i != -1)
        {
            InsertAtIndex(markupNode, i + 1, i + 1);

            if (i + 2 >= _nodes.Count)
                break;

            i = _nodes.FindIndex(i + 2, x => x.Name == tagText && x.Closing);
        }
    }

    /// <summary>
    /// Helper function that inserts a node inside of other nodes of a specific type.
    /// The new node encloses any other nodes that the target node encloses.
    /// </summary>
    /// <param name="markupNode">The node to be inserted; may not be a text node.</param>
    /// <param name="tagText">The tag to search for.</param>
    public void InsertInsideTag(MarkupNode markupNode, string tagText)
    {
        ArgumentNullException.ThrowIfNull(markupNode.Name);

        var openingNodeCount = _nodes.Count(x => x.Name == tagText && !x.Closing);
        var closingNodeCount = _nodes.Count(x => x.Name == tagText && x.Closing);

        if (openingNodeCount != closingNodeCount)
            throw new Exception("Opening and Closing node count mismatch.");

        if (openingNodeCount == 0)
            return;

        var i = _nodes.FindIndex(x => x.Name == tagText && !x.Closing);
        while (i > -1)
        {
            _nodes.Insert(i + 1, markupNode);

            if (i + 2 >= _nodes.Count)
                break;

            i = _nodes.FindIndex(i + 2, x => x.Name == tagText && !x.Closing);
        }

        var j = _nodes.FindIndex(x => x.Name == tagText && x.Closing);
        while (j > -1)
        {
            _nodes.Insert(j, new MarkupNode(markupNode.Name, null, null, true));

            if (j + 2 >= _nodes.Count)
                break;

            j = _nodes.FindIndex(j + 2, x => x.Name == tagText && x.Closing);
        }
    }

    /// <summary>
    /// Helper function that inserts a node around other nodes of a specific type.
    /// The new node encloses any other nodes that the target node encloses.
    /// </summary>
    /// <param name="markupNode">The node to be inserted; may not be a text node.</param>
    /// <param name="tagText">The tag to search for.</param>
    public void InsertOutsideTag(MarkupNode markupNode, string tagText)
    {
        ArgumentNullException.ThrowIfNull(markupNode.Name);

        var openingNodeCount = _nodes.Count(x => x.Name == tagText && !x.Closing);
        var closingNodeCount = _nodes.Count(x => x.Name == tagText && x.Closing);

        if (openingNodeCount != closingNodeCount)
            throw new Exception("Opening and Closing node count mismatch.");

        if (openingNodeCount == 0)
            return;

        var i = _nodes.FindIndex(x => x.Name == tagText && !x.Closing);
        while (i > -1)
        {
            _nodes.Insert(i, markupNode);

            if (i + 2 >= _nodes.Count)
                break;

            i = _nodes.FindIndex(i + 2, x => x.Name == tagText && !x.Closing);
        }

        var j = _nodes.FindIndex(x => x.Name == tagText && x.Closing);
        while (j > -1)
        {
            _nodes.Insert(j + 1, new MarkupNode(markupNode.Name, null, null, true));

            if (j + 2 >= _nodes.Count)
                break;

            j = _nodes.FindIndex(j + 2, x => x.Name == tagText && x.Closing);
        }
    }

    /// <summary>
    /// Helper function that inserts a node before the entire message.
    /// </summary>
    /// <param name="markupNode">The node to be inserted.</param>
    public void InsertBeforeMessage(MarkupNode markupNode)
    {
        InsertAtIndex(markupNode, 0, 0);
    }

    /// <summary>
    /// Helper function that inserts a node after the entire message.
    /// </summary>
    /// <param name="markupNode">The node to be inserted.</param>
    public void InsertAfterMessage(MarkupNode markupNode)
    {
        PushTag(markupNode, true);
    }

    /// <summary>
    /// Helper function that tries to find the first instance of a tag and returns a FormattedMessage containing the nodes inside.
    /// </summary>
    /// <param name="returnMessage">The message inside of the chosen tag.</param>
    /// <param name="tagText">The tag to search for.</param>
    public bool TryGetMessageInsideTag(out FormattedMessage? returnMessage, string tagText)
    {
        returnMessage = null;

        var openingNode = _nodes.FindIndex(x => x.Name == tagText && !x.Closing);
        var nextNode = openingNode;
        var nodeCount = 1;

        if (openingNode == -1)
            return false;

        while (nodeCount > 0)
        {
            nextNode = _nodes.FindIndex(nextNode + 1, x => x.Name == tagText);
            if (!_nodes[nextNode].Closing)
            {
                nodeCount++;
            }
            else
            {
                nodeCount--;
            }
        }

        if (nextNode == -1)
            return false;

        var resultingRange = _nodes.GetRange(openingNode, nextNode - openingNode + 1);
        returnMessage = new FormattedMessage(resultingRange);
        return true;
    }

    # endregion

    /// <summary>
    /// Returns an enumerator that enumerates every rune for each text node contained in this formatted text instance.
    /// </summary>
    public FormattedMessageRuneEnumerator EnumerateRunes()
    {
        return new FormattedMessageRuneEnumerator(this);
    }

    public NodeEnumerator GetEnumerator()
    {
        return new NodeEnumerator(_nodes.GetEnumerator());
    }

    IEnumerator<MarkupNode> IEnumerable<MarkupNode>.GetEnumerator()
    {
        return GetEnumerator();
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

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <returns>The string without filtering out markup tags.</returns>
    public string ToMarkup()
    {
        return string.Join("", _nodes);
    }

    public struct FormattedMessageRuneEnumerator : IEnumerable<Rune>, IEnumerator<Rune>
    {
        private readonly FormattedMessage _msg;
        private List<MarkupNode>.Enumerator _tagEnumerator;
        private StringRuneEnumerator _runeEnumerator;

        internal FormattedMessageRuneEnumerator(FormattedMessage msg)
        {
            _msg = msg;
            _tagEnumerator = msg._nodes.GetEnumerator();
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
            _tagEnumerator = _msg._nodes.GetEnumerator();
            _runeEnumerator = "".EnumerateRunes();
        }

        public Rune Current => _runeEnumerator.Current;

        object IEnumerator.Current => Current;

        void IDisposable.Dispose()
        {
        }
    }

    public struct NodeEnumerator : IEnumerator<MarkupNode>
    {
        private List<MarkupNode>.Enumerator _enumerator;

        internal NodeEnumerator(List<MarkupNode>.Enumerator enumerator)
        {
            _enumerator = enumerator;
        }

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        void IEnumerator.Reset()
        {
            ((IEnumerator) _enumerator).Reset();
        }

        public MarkupNode Current => _enumerator.Current;

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            _enumerator.Dispose();
        }
    }
}
