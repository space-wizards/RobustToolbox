using System;
using System.Collections.Generic;
using System.IO;
using Robust.Shared.Collections;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Robust.Shared.Serialization.Markdown;

// YDN has broken nullable annotations. Yeppers.
#nullable disable

public static class DataNodeParser
{
    public static IEnumerable<DataNodeDocument> ParseYamlStream(TextReader reader)
    {
        return ParseYamlStream(new Parser(reader));
    }

    internal static IEnumerable<DataNodeDocument> ParseYamlStream(Parser parser)
    {
        parser.Consume<StreamStart>();

        while (!parser.TryConsume<StreamEnd>(out _))
        {
            yield return ParseDocument(parser);
        }
    }

    private static DataNodeDocument ParseDocument(Parser parser)
    {
        var state = new DocumentState();

        parser.Consume<DocumentStart>();

        var root = Parse(parser, state);

        parser.Consume<DocumentEnd>();

        ResolveAliases(state);

        return new DataNodeDocument(root);
    }

    private static DataNode Parse(Parser parser, DocumentState state)
    {
        if (parser.Current is Scalar)
            return ParseValue(parser, state);

        if (parser.Current is SequenceStart)
            return ParseSequence(parser, state);

        if (parser.Current is MappingStart)
            return ParseMapping(parser, state);

        if (parser.Current is AnchorAlias)
            return ParseAlias(parser, state);

        throw new NotSupportedException();
    }

    private static DataNode ParseAlias(Parser parser, DocumentState state)
    {
        var alias = parser.Consume<AnchorAlias>();

        if (!state.Anchors.TryGetValue(alias.Value, out var node))
        {
            // Don't have this anchor yet. It may be defined later in the document.
            return new DataNodeAlias(alias.Value);
        }

        return node;
    }

    private static ValueDataNode ParseValue(Parser parser, DocumentState state)
    {
        var ev = parser.Consume<Scalar>();
        var node = new ValueDataNode(ev.Value);
        node.Tag = ConvertTag(ev.Tag);
        node.Start = ev.Start;
        node.End = ev.End;

        NodeParsed(node, ev, false, state);

        return node;
    }

    private static SequenceDataNode ParseSequence(Parser parser, DocumentState state)
    {
        var ev = parser.Consume<SequenceStart>();

        var node = new SequenceDataNode();
        node.Tag = ConvertTag(ev.Tag);
        node.Start = ev.Start;

        var unresolvedAlias = false;

        SequenceEnd seqEnd;
        while (!parser.TryConsume(out seqEnd))
        {
            var value = Parse(parser, state);

            node.Add(value);

            unresolvedAlias |= value is DataNodeAlias;
        }

        node.End = seqEnd.End;

        NodeParsed(node, ev, unresolvedAlias, state);

        return node;
    }

    private static MappingDataNode ParseMapping(Parser parser, DocumentState state)
    {
        var ev = parser.Consume<MappingStart>();

        var node = new MappingDataNode();
        node.Tag = ConvertTag(ev.Tag);

        var unresolvedAlias = false;

        MappingEnd mapEnd;
        while (!parser.TryConsume(out mapEnd))
        {
            var key = Parse(parser, state);
            var value = Parse(parser, state);

            node.Add(key, value);

            unresolvedAlias |= key is DataNodeAlias;
            unresolvedAlias |= value is DataNodeAlias;
        }

        node.End = mapEnd.End;

        NodeParsed(node, ev, unresolvedAlias, state);

        return node;
    }

    private static void NodeParsed(DataNode node, NodeEvent ev, bool unresolvedAlias, DocumentState state)
    {
        if (unresolvedAlias)
            state.UnresolvedAliasOwners.Add(node);

        if (ev.Anchor.IsEmpty)
            return;

        if (state.Anchors.ContainsKey(ev.Anchor))
            throw new DataParseException($"Duplicate anchor defined in document: {ev.Anchor}");

        state.Anchors[ev.Anchor] = node;
    }

    private static void ResolveAliases(DocumentState state)
    {
        foreach (var node in state.UnresolvedAliasOwners)
        {
            switch (node)
            {
                case MappingDataNode mapping:
                    ResolveMappingAliases(mapping, state);
                    break;

                case SequenceDataNode sequence:
                    ResolveSequenceAliases(sequence, state);
                    break;
            }
        }
    }

    private static void ResolveMappingAliases(MappingDataNode mapping, DocumentState state)
    {
        var swaps = new ValueList<(DataNode key, DataNode value)>();

        foreach (var (key, value) in mapping)
        {
            if (key is not DataNodeAlias && value is not DataNodeAlias)
                return;

            var newKey = key is DataNodeAlias keyAlias ? ResolveAlias(keyAlias, state) : key;
            var newValue = value is DataNodeAlias valueAlias ? ResolveAlias(valueAlias, state) : value;

            swaps.Add((newKey, newValue));
            mapping.Remove(key);
        }

        foreach (var (key, value) in swaps)
        {
            mapping[key] = value;
        }
    }

    private static void ResolveSequenceAliases(SequenceDataNode sequence, DocumentState state)
    {
        for (var i = 0; i < sequence.Count; i++)
        {
            if (sequence[i] is DataNodeAlias alias)
                sequence[i] = ResolveAlias(alias, state);
        }
    }

    private static DataNode ResolveAlias(DataNodeAlias alias, DocumentState state)
    {
        if (!state.Anchors.TryGetValue(alias.Anchor, out var node))
            throw new DataParseException($"Unable to resolve alias '{alias.Anchor}'");

        return node;
    }

    private static string ConvertTag(TagName tag)
    {
        return (tag.IsNonSpecific || tag.IsEmpty) ? null : tag.Value;
    }

    private sealed class DocumentState
    {
        public readonly Dictionary<AnchorName, DataNode> Anchors = new();
        public ValueList<DataNode> UnresolvedAliasOwners;
    }

    private sealed class DataNodeAlias : DataNode
    {
        public readonly AnchorName Anchor;

        public DataNodeAlias(AnchorName anchor) : base(default, default)
        {
            Anchor = anchor;
        }

        public override bool IsEmpty => true;

        public override DataNode Copy()
        {
            throw new NotSupportedException();
        }

        public override DataNode Except(DataNode node)
        {
            throw new NotSupportedException();
        }

        public override DataNode PushInheritance(DataNode parent)
        {
            throw new NotSupportedException();
        }
    }
}

public sealed class DataParseException : Exception
{
    public DataParseException()
    {
    }

    public DataParseException(string message) : base(message)
    {
    }

    public DataParseException(string message, Exception inner) : base(message, inner)
    {
    }
}

public sealed record DataNodeDocument(DataNode Root);

