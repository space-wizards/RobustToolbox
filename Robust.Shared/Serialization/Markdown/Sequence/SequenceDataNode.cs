using System;
using System.Collections;
using System.Collections.Generic;
using Robust.Shared.Serialization.Markdown.Value;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization.Markdown.Sequence
{
    public sealed class SequenceDataNode : DataNode<SequenceDataNode>, IList<DataNode>
    {
        private readonly List<DataNode> _nodes;

        public SequenceDataNode() : base(NodeMark.Invalid, NodeMark.Invalid)
        {
            _nodes = new();
        }

        public SequenceDataNode(int size) : base(NodeMark.Invalid, NodeMark.Invalid)
        {
            _nodes = new(size);
        }

        public SequenceDataNode(List<DataNode> nodes) : base(NodeMark.Invalid, NodeMark.Invalid)
        {
            _nodes = nodes;
        }

        public SequenceDataNode(List<string> values) : base(NodeMark.Invalid, NodeMark.Invalid)
        {
            _nodes = new(values.Count);
            foreach (var value in values)
            {
                _nodes.Add(new ValueDataNode(value));
            }
        }

        public SequenceDataNode(YamlSequenceNode sequence) : base(sequence.Start, sequence.End)
        {
            _nodes = new(sequence.Children.Count);
            foreach (var node in sequence.Children)
            {
                _nodes.Add(node.ToDataNode());
            }

            Tag = sequence.Tag.IsEmpty ? null : sequence.Tag.Value;
        }

        public SequenceDataNode(params DataNode[] nodes) : base(NodeMark.Invalid, NodeMark.Invalid)
        {
            _nodes = new(nodes.Length);
            foreach (var node in nodes)
            {
                _nodes.Add(node);
            }
        }

        public SequenceDataNode(params string[] strings) : base(NodeMark.Invalid, NodeMark.Invalid)
        {
            _nodes = new(strings.Length);
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

        public override bool IsEmpty => _nodes.Count == 0;

        public override SequenceDataNode Copy()
        {
            var newSequence = new SequenceDataNode(Sequence.Count)
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

        public List<DataNode>.Enumerator GetEnumerator() => _nodes.GetEnumerator();
        IEnumerator<DataNode> IEnumerable<DataNode>.GetEnumerator() => _nodes.GetEnumerator();

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
            var newList = new List<DataNode>();
            foreach (var nodeNode in _nodes)
            {
                if (!node._nodes.Contains(nodeNode)) newList.Add(nodeNode);
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

        public override bool Equals(object? obj)
        {
            if (obj is not SequenceDataNode other)
                return false;

            if (_nodes.Count != other._nodes.Count)
                return false;

            // We cannot just use Except() to check equality, because the sequence [a, a, b] would be equivalent to
            // [a, b ,b]. I.e., the number of entries matter. Similarly, for anyone serializing an ordered list, the
            // order of entries matters.

            for (int i = 0; i < _nodes.Count; i++)
            {
                if (!_nodes[i].Equals(other._nodes[i]))
                    return false;
            }
            return true;
        }

        [Obsolete("Use SerializationManager.PushComposition()")]
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
