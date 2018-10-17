using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

namespace SS14.Client.Utility
{
    /// <summary>
    ///     Parser for Godot asset files.
    /// </summary>
    internal class GodotParser
    {
        private TextParser _parser;

        /// <summary>
        ///     Parse a Godot .tscn or .tres file's contents into a <see cref="GodotAsset"/>.
        /// </summary>
        /// <param name="reader">A text reader reading the resource file contents.</param>
        public static GodotAsset Parse(TextReader reader)
        {
            var parser = new GodotParser();
            return parser._parse(reader);
        }

        private GodotAsset _parse(TextReader reader)
        {
            try
            {
                return _parseInternal(reader);
            }
            catch (Exception e)
            {
                if (_parser == null)
                {
                    throw;
                }

                throw new TextParser.ParserException(
                    $"Exception while parsing at ({_parser.CurrentLine}, {_parser.CurrentIndex})", e);
            }
        }

        private GodotAsset _parseInternal(TextReader reader)
        {
            _parser = new TextParser(reader);
            _parser.NextLine();
            _parser.Parse('[');
            if (_parser.TryParse("gd_scene"))
            {
                // nothing yet.
            }
            else if (_parser.TryParse("gd_resource"))
            {
                throw new NotImplementedException("We only bother to read scenes right now.");
            }
            else
            {
                throw new TextParser.ParserException("Expected gd_scene or gd_resource");
            }

            var extResources = new List<GodotAsset.ExtResourceRef>();
            var nodes = new List<NodeHeader>();

            // Go over all the [] headers in the file.
            while (!_parser.IsEOF())
            {
                _parser.NextLine();
                _parser.EatWhitespace();
                if (_parser.IsEOL())
                {
                    continue;
                }

                // Skip anything that isn't a header.
                // Yes this means all node/resource properties are ignored.
                // I don't need them at the moment.
                if (!_parser.TryParse('['))
                {
                    continue;
                }

                _parser.EatWhitespace();

                if (_parser.TryParse("node"))
                {
                    nodes.Add(ParseNodeHeader());
                }

                else if (_parser.TryParse("ext_resource"))
                {
                    extResources.Add(ParseExtResourceRef());
                }

                else
                {
                    // Probably something like sub_resource or whatever. Ignore it.
                    continue;
                }

                _parser.Parse(']');
                _parser.EnsureEOL();
            }

            // Alright try to resolve tree graph.
            // Sort based on tree depth by parsing parent path.
            // This way, when doing straight iteration, we'll always have the parent.


            var finalNodes = nodes
                .Select(n => new GodotAssetScene.NodeDef(n.Name, n.Type, n.Parent, n.Index, n.Instance))
                .ToList();

            finalNodes.Sort(GodotAssetScene.NodeDef.FlattenedTreeComparer);

            return new GodotAssetScene(finalNodes, extResources);
        }

        private NodeHeader ParseNodeHeader()
        {
            _parser.EatWhitespace();

            string name = null;
            string type = null;
            string index = null;
            string parent = null;
            GodotAsset.TokenExtResource? instance = null;

            while (_parser.Peek() != ']')
            {
                var (keyName, value) = ParseKeyValue();

                switch (keyName)
                {
                    case "name":
                        name = (string) value;
                        break;
                    case "type":
                        type = (string) value;
                        break;
                    case "index":
                        index = (string) value;
                        break;
                    case "parent":
                        parent = (string) value;
                        break;
                    case "instance":
                        instance = (GodotAsset.TokenExtResource) value;
                        break;
                }

                _parser.EatWhitespace();
            }

            return new NodeHeader(name, type, index == null ? 0 : int.Parse(index, CultureInfo.InvariantCulture),
                parent, instance);
        }

        private GodotAsset.ExtResourceRef ParseExtResourceRef()
        {
            _parser.EatWhitespace();

            string path = null;
            string type = null;
            var id = 0L;

            while (_parser.Peek() != ']')
            {
                var (keyName, value) = ParseKeyValue();

                switch (keyName)
                {
                    case "path":
                        path = (string) value;
                        break;
                    case "type":
                        type = (string) value;
                        break;
                    case "id":
                        id = (long) value;
                        break;
                }

                _parser.EatWhitespace();
            }

            return new GodotAsset.ExtResourceRef(path, type, id);
        }

        private (string name, object value) ParseKeyValue()
        {
            _parser.EatWhitespace();
            var keyList = new List<char>();

            while (true)
            {
                _parser.EnsureNoEOL();

                // Eat until = or whitespace.
                if (_parser.Peek() == '=' || _parser.EatWhitespace())
                {
                    break;
                }

                keyList.Add(_parser.Take());
            }

            var name = new string(keyList.ToArray());

            _parser.Parse('=');
            _parser.EatWhitespace();

            return (name, ParseGodotValue());
        }

        private object ParseGodotValue()
        {
            if (_parser.Peek() == '"')
            {
                return ParseGodotString();
            }

            if (_parser.PeekIsDigit())
            {
                return ParseGodotNumber();
            }

            if (_parser.TryParse("ExtResource("))
            {
                _parser.EatWhitespace();
                var val = new GodotAsset.TokenExtResource((long) ParseGodotNumber());
                _parser.EatWhitespace();
                _parser.Parse(')');
                return val;
            }

            throw new NotImplementedException($"Unable to handle complex kv pairs: '{_parser.Peek()}'");
        }

        private string ParseGodotString()
        {
            _parser.Parse('"');
            var list = new List<char>();
            var escape = false;

            while (true)
            {
                _parser.EnsureNoEOL();

                var value = _parser.Take();
                if (value == '\\')
                {
                    if (escape)
                    {
                        list.Add('\\');
                    }
                    else
                    {
                        escape = true;
                    }

                    continue;
                }

                if (value == '"' && !escape)
                {
                    break;
                }

                if (escape)
                {
                    throw new TextParser.ParserException("Unknown escape sequence");
                }
                else
                {
                    list.Add(value);
                }
            }

            return new string(list.ToArray());
        }

        private object ParseGodotNumber()
        {
            var list = new List<char>();

            while (!_parser.IsEOL())
            {
                if (!_parser.PeekIsDigit() && _parser.Peek() != '.')
                {
                    break;
                }

                list.Add(_parser.Take());
            }

            var number = new string(list.ToArray());

            if (number.IndexOf('.') != -1)
            {
                return float.Parse(number, CultureInfo.InvariantCulture);
            }

            return long.Parse(number, CultureInfo.InvariantCulture);
        }

        private readonly struct NodeHeader
        {
            public readonly string Name;
            public readonly string Type;
            public readonly int Index;
            public readonly string Parent;
            public readonly GodotAsset.TokenExtResource? Instance;

            public NodeHeader(string name, string type, int index, string parent, GodotAsset.TokenExtResource? instance)
            {
                Name = name;
                Type = type;
                Index = index;
                Parent = parent;
                Instance = instance;
            }
        }

        private class TextParser
        {
            private readonly TextReader _reader;

            public int CurrentLine { get; private set; }

            /// <summary>
            ///     Index of the next character to be read.
            /// </summary>
            public int CurrentIndex { get; private set; }

            private string _currentLine;

            public TextParser(TextReader reader)
            {
                _reader = reader;
            }

            public void NextLine()
            {
                _currentLine = _reader.ReadLine();
                CurrentIndex = 0;
                CurrentLine++;
            }

            public bool TryParse(string str)
            {
                if (IsEOL())
                {
                    return false;
                }

                var valid = _currentLine.IndexOf(str, CurrentIndex, StringComparison.Ordinal) == CurrentIndex;

                if (valid)
                {
                    Advance(str.Length);
                }

                return valid;
            }

            public bool TryParse(char chr)
            {
                if (IsEOL())
                {
                    return false;
                }

                var valid = _currentLine[CurrentIndex] == chr;
                if (valid)
                {
                    Advance();
                }

                return valid;
            }

            public void Parse(char chr)
            {
                if (IsEOL())
                {
                    throw new ParserException($"Expected '{chr}', got EOL");
                }

                if (_currentLine[CurrentIndex] != chr)
                {
                    throw new ParserException($"Expected '{chr}'.");
                }

                Advance();
            }

            [System.Diagnostics.Contracts.Pure]
            public bool IsEOL()
            {
                return _currentLine == null || _currentLine.Length <= CurrentIndex;
            }

            [System.Diagnostics.Contracts.Pure]
            public bool IsEOF()
            {
                return _reader.Peek() == -1 && IsEOL();
            }

            public void EnsureEOL()
            {
                if (!IsEOL())
                {
                    throw new ParserException("Expected EOL");
                }
            }

            public void EnsureNoEOL()
            {
                if (IsEOL())
                {
                    throw new ParserException("Unexpected EOL");
                }
            }

            public bool EatWhitespace()
            {
                var ateAny = false;
                while (!IsEOL())
                {
                    if (!char.IsWhiteSpace(_currentLine, CurrentIndex))
                    {
                        break;
                    }

                    Advance();
                    ateAny = true;
                }

                return ateAny;
            }

            [System.Diagnostics.Contracts.Pure]
            public char Peek()
            {
                return _currentLine[CurrentIndex];
            }

            public char Take()
            {
                return _currentLine[CurrentIndex++];
            }

            public void Advance(int amount = 1)
            {
                CurrentIndex += amount;
            }

            // Wrapping for the various IsXXX methods on Char because Char is dumb.
            [System.Diagnostics.Contracts.Pure]
            public bool PeekIsDigit()
            {
                return char.IsDigit(_currentLine, CurrentIndex);
            }

            public class ParserException : Exception
            {
                public ParserException(string message) : base(message)
                {
                }

                public ParserException(string message, Exception inner) : base(message, inner)
                {
                }
            }
        }
    }

    /// <summary>
    ///     Represents a Godot .(t)res or .(t)scn resource file that we loaded manually.
    /// </summary>
    internal abstract class GodotAsset
    {
        protected GodotAsset(IList<ExtResourceRef> extResources)
        {
            ExtResources = extResources;
        }

        /// <summary>
        ///     A list of all the external resources that are referenced by this resource.
        /// </summary>
        public IList<ExtResourceRef> ExtResources { get; }

        public ExtResourceRef GetExtResource(TokenExtResource token)
        {
            return ExtResources[(int)token.ResourceId - 1];
        }

        /// <summary>
        ///     A token value to indicate "this is a reference to an external resource".
        /// </summary>
        public readonly struct TokenExtResource : IEquatable<TokenExtResource>
        {
            public readonly long ResourceId;

            public TokenExtResource(long resourceId)
            {
                ResourceId = resourceId;
            }

            public bool Equals(TokenExtResource other)
            {
                return ResourceId == other.ResourceId;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is TokenExtResource other && Equals(other);
            }

            public override int GetHashCode()
            {
                return ResourceId.GetHashCode();
            }
        }

        /// <summary>
        ///     A reference to an external resource.
        /// </summary>
        public class ExtResourceRef
        {
            /// <summary>
            ///     The godot file path of the external resource,
            ///     usually prefixed with res://
            /// </summary>
            public string Path { get; }

            /// <summary>
            ///     The Godot type of the referenced resource.
            ///     This is NOT a .NET type!
            /// </summary>
            public string Type { get; }

            /// <summary>
            ///     The ID of this external resource, so what <see cref="TokenExtResource"/> stores.
            /// </summary>
            public long Id { get; }

            public ExtResourceRef(string path, string type, long id)
            {
                Path = path;
                Type = type;
                Id = id;
            }
        }
    }

    /// <inheritdoc />
    /// <summary>
    ///     This type is specifically a loaded .tscn file.
    /// </summary>
    internal class GodotAssetScene : GodotAsset
    {
        public NodeDef RootNode => Nodes[0];
        public List<NodeDef> Nodes { get; }

        public GodotAssetScene(List<NodeDef> nodes, List<ExtResourceRef> extResourceRefs) : base(extResourceRefs)
        {
            Nodes = nodes;
        }

        public class NodeDef
        {
            /// <summary>
            ///     The name of this node.
            /// </summary>
            [NotNull]
            public string Name { get; }

            /// <summary>
            ///     The type of this node.
            ///     Can be null if this node is actually an instance (or part of one).
            /// </summary>
            [CanBeNull]
            public string Type { get; }

            /// <summary>
            ///     The scene-relative parent of this node.
            /// </summary>
            [CanBeNull]
            public string Parent { get; }

            /// <summary>
            ///     Index of this node among its siblings.
            /// </summary>
            public int Index { get; }

            /// <summary>
            ///     An external resource reference pointing to the scene we are instancing, if any.
            /// </summary>
            public TokenExtResource? Instance { get; }

            public NodeDef(string name, [CanBeNull] string type, [CanBeNull] string parent, int index, TokenExtResource? instance)
            {
                Name = name;
                Type = type;
                Parent = parent;
                Index = index;
                Instance = instance;
            }

            private sealed class FlattenedTreeComparerImpl : IComparer<NodeDef>
            {
                public int Compare(NodeDef x, NodeDef y)
                {
                    if (ReferenceEquals(x, y)) return 0;
                    if (ReferenceEquals(null, y)) return 1;
                    if (ReferenceEquals(null, x)) return -1;

                    var parentComparison = ParentFieldWeight(x.Parent).CompareTo(ParentFieldWeight(y.Parent));
                    if (parentComparison != 0) return parentComparison;
                    var indexComparison = x.Index.CompareTo(y.Index);
                    if (indexComparison != 0) return indexComparison;
                    return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
                }

                private static int ParentFieldWeight(string parentField)
                {
                    switch (parentField)
                    {
                        case null:
                            return 0;
                        case ".":
                            return 1;
                        default:
                            return parentField.Count(c => c == '/') + 2;
                    }
                }
            }

            public static IComparer<NodeDef> FlattenedTreeComparer { get; } = new FlattenedTreeComparerImpl();
        }
    }
}
