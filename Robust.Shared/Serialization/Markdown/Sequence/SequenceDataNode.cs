using System;
using System.Collections;
using System.Collections.Generic;
using Robust.Shared.Serialization.Markdown.Value;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization.Markdown.Sequence
{
    public sealed class SequenceDataNode : DataNode<SequenceDataNode>, IList<DataNode>
    {
        private readonly List<DataNode> _nodes = new();

        public SequenceDataNode() : base(NodeMark.Invalid, NodeMark.Invalid) { }

        public SequenceDataNode(List<DataNode> nodes) : this()
        {
            _nodes = nodes;
        }

        public SequenceDataNode(List<string> values) : this()
        {
            foreach (var value in values)
            {
                _nodes.Add(new ValueDataNode(value));
            }
        }

        public SequenceDataNode(YamlSequenceNode sequence) : base(sequence.Start, sequence.End)
        {
            foreach (var node in sequence.Children)
            {
                _nodes.Add(node.ToDataNode());
            }

            Tag = sequence.Tag;
        }

        public SequenceDataNode(params DataNode[] nodes) : this()
        {
            foreach (var node in nodes)
            {
                _nodes.Add(node);
            }
        }

        public SequenceDataNode(params string[] strings) : this()
        {
            foreach (var s in strings)
            {
                _nodes.Add(new ValueDataNode(s));
            }
        }

        public YamlSequenceNode ToSequenceNode()
        {
            var node = new YamlSequenceNode();
            foreach (var dataNode in _nodes)
            {
                node.Children.Add(dataNode.ToYamlNode());
            }

            node.Tag = Tag;

            return node;
        }

        public IReadOnlyList<DataNode> Sequence => _nodes;

        public int IndexOf(DataNode item) => _nodes.IndexOf(item);

        public void Insert(int index, DataNode item) => _nodes.Insert(index, item);

        public void RemoveAt(int index) => _nodes.RemoveAt(index);

        public DataNode this[int index]
        {
            get => _nodes[index];
            set => _nodes[index] = value;
        }

        public void Add(DataNode node)
        {
            _nodes.Add(node);
        }

        public void Clear() => _nodes.Clear();

        public bool Contains(DataNode item) => _nodes.Contains(item);

        public void CopyTo(DataNode[] array, int arrayIndex) => _nodes.CopyTo(array, arrayIndex);

        public bool Remove(DataNode node)
        {
            return _nodes.Remove(node);
        }

        public int Count => _nodes.Count;
        public bool IsReadOnly => false;

        public T Cast<T>(int index) where T : DataNode
        {
            return (T) this[index];
        }

        public override SequenceDataNode Copy()
        {
            var newSequence = new SequenceDataNode()
            {
                Tag = Tag,
                Start = Start,
                End = End
            };

            foreach (var node in Sequence)
            {
                newSequence.Add(node.Copy());
            }

            return newSequence;
        }

        public IEnumerator<DataNode> GetEnumerator() => _nodes.GetEnumerator();

        public override int GetHashCode()
        {
            var code = new HashCode();
            foreach (var dataNode in _nodes)
            {
                code.Add(dataNode);
            }

            return code.ToHashCode();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override SequenceDataNode? Except(SequenceDataNode node)
        {
            var set = new HashSet<DataNode>(node._nodes);
            var newList = new List<DataNode>();
            foreach (var nodeNode in node._nodes)
            {
                if (!set.Contains(nodeNode)) newList.Add(nodeNode);
            }

            if (newList.Count > 0)
            {
                return new SequenceDataNode(newList)
                {
                    Tag = Tag,
                    Start = Start,
                    End = End
                };
            }

            return null;
        }

        public override SequenceDataNode PushInheritance(SequenceDataNode node)
        {
            var newNode = Copy();
            foreach (var val in node)
            {
                newNode.Add(val.Copy());
            }

            return newNode;
        }
    }
}
