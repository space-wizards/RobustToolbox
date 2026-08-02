using System;
using System.Collections;
using System.Collections.Generic;
using Robust.Shared.Serialization.Markdown.Value;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization.Markdown.Sequence
{
    /// <summary>
    /// A yaml sequence. Parsed and composed sequences retain a tight array; mutation materializes a List on demand.
    /// </summary>
    public sealed class SequenceDataNode : DataNode<SequenceDataNode>, IList<DataNode>
    {
        private object _nodes;

        private bool IsArray => _nodes is DataNode[];
        private DataNode[] Array => (DataNode[]) _nodes;
        private List<DataNode> List => (List<DataNode>) _nodes;

        public SequenceDataNode() : this(0)
        {
        }

        public SequenceDataNode(int size) : base(NodeMark.Invalid, NodeMark.Invalid)
        {
            _nodes = new List<DataNode>(size);
        }

        public SequenceDataNode(List<DataNode> nodes) : base(NodeMark.Invalid, NodeMark.Invalid)
        {
            _nodes = nodes;
        }

        public SequenceDataNode(List<string> values) : base(NodeMark.Invalid, NodeMark.Invalid)
        {
            var nodes = new DataNode[values.Count];
            for (var i = 0; i < values.Count; i++)
            {
                nodes[i] = new ValueDataNode(values[i]);
            }

            _nodes = nodes;
        }

        public SequenceDataNode(YamlSequenceNode sequence) : base(sequence.Start, sequence.End)
        {
            var nodes = new DataNode[sequence.Children.Count];
            for (var i = 0; i < nodes.Length; i++)
            {
                nodes[i] = sequence.Children[i].ToDataNode();
            }

            _nodes = nodes;
            Tag = sequence.Tag.IsEmpty ? null : sequence.Tag.Value;
        }

        public SequenceDataNode(params DataNode[] nodes) : base(NodeMark.Invalid, NodeMark.Invalid)
        {
            _nodes = (DataNode[]) nodes.Clone();
        }

        public SequenceDataNode(params string[] strings) : base(NodeMark.Invalid, NodeMark.Invalid)
        {
            var nodes = new DataNode[strings.Length];
            for (var i = 0; i < strings.Length; i++)
            {
                nodes[i] = new ValueDataNode(strings[i]);
            }

            _nodes = nodes;
        }

        private SequenceDataNode(DataNode[] nodes, bool takeOwnership) : base(NodeMark.Invalid, NodeMark.Invalid)
        {
            _nodes = nodes;
        }

        public YamlSequenceNode ToSequenceNode()
        {
            var node = new YamlSequenceNode();
            foreach (var dataNode in this)
            {
                node.Children.Add(dataNode.ToYamlNode());
            }

            node.Tag = Tag;

            return node;
        }

        public IReadOnlyList<DataNode> Sequence => (IReadOnlyList<DataNode>) _nodes;

        public int IndexOf(DataNode item)
        {
            return IsArray ? System.Array.IndexOf(Array, item) : List.IndexOf(item);
        }

        public void Insert(int index, DataNode item) => EnsureList().Insert(index, item);

        public void RemoveAt(int index) => EnsureList().RemoveAt(index);

        public DataNode this[int index]
        {
            get => IsArray ? Array[index] : List[index];
            set
            {
                if (IsArray)
                    Array[index] = value;
                else
                    List[index] = value;
            }
        }

        public void Add(DataNode node)
        {
            EnsureList().Add(node);
        }

        public void Clear() => EnsureList().Clear();

        public bool Contains(DataNode item)
        {
            return IsArray ? System.Array.IndexOf(Array, item) != -1 : List.Contains(item);
        }

        public void CopyTo(DataNode[] array, int arrayIndex)
        {
            if (IsArray)
                System.Array.Copy(Array, 0, array, arrayIndex, Array.Length);
            else
                List.CopyTo(array, arrayIndex);
        }

        public bool Remove(DataNode node)
        {
            return EnsureList().Remove(node);
        }

        public int Count => IsArray ? Array.Length : List.Count;
        public bool IsReadOnly => false;

        public T Cast<T>(int index) where T : DataNode
        {
            return (T) this[index];
        }

        public override bool IsEmpty => Count == 0;

        public override SequenceDataNode Copy()
        {
            var nodes = new DataNode[Count];
            for (var i = 0; i < nodes.Length; i++)
            {
                nodes[i] = this[i].Copy();
            }

            return new SequenceDataNode(nodes, takeOwnership: true)
            {
                Tag = Tag,
                Start = Start,
                End = End
            };
        }

        /// <summary>
        /// Variant of <see cref="Copy"/> that doesn't clone the child nodes.
        /// </summary>
        public SequenceDataNode ShallowClone()
        {
            var nodes = new DataNode[Count];
            for (var i = 0; i < nodes.Length; i++)
            {
                nodes[i] = this[i];
            }

            return new SequenceDataNode(nodes, takeOwnership: true)
            {
                Tag = Tag,
                Start = Start,
                End = End
            };
        }

        internal void Seal()
        {
            if (!IsArray)
                _nodes = List.ToArray();
        }

        private List<DataNode> EnsureList()
        {
            if (!IsArray)
                return List;

            var list = new List<DataNode>(Array);
            _nodes = list;
            return list;
        }

        public Enumerator GetEnumerator() => new(this);
        IEnumerator<DataNode> IEnumerable<DataNode>.GetEnumerator() => GetEnumerator();

        public override int GetHashCode()
        {
            var code = new HashCode();
            foreach (var dataNode in this)
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
            foreach (var nodeNode in this)
            {
                if (!node.Contains(nodeNode))
                    newList.Add(nodeNode);
            }

            if (newList.Count == 0)
                return null;

            return new SequenceDataNode(newList.ToArray(), takeOwnership: true)
            {
                Tag = Tag,
                Start = Start,
                End = End
            };
        }

        public override bool Equals(object? obj)
        {
            if (obj is not SequenceDataNode other)
                return false;

            if (Count != other.Count)
                return false;

            // We cannot just use Except() to check equality, because the sequence [a, a, b] would be equivalent to
            // [a, b, b]. I.e., the number of entries matter. Similarly, for anyone serializing an ordered list, the
            // order of entries matters.
            for (var i = 0; i < Count; i++)
            {
                if (!this[i].Equals(other[i]))
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

        public struct Enumerator : IEnumerator<DataNode>
        {
            private readonly DataNode[]? _array;
            private List<DataNode>.Enumerator _listEnumerator;
            private int _index;

            internal Enumerator(SequenceDataNode sequence)
            {
                if (sequence.IsArray)
                {
                    _array = sequence.Array;
                    _listEnumerator = default;
                }
                else
                {
                    _array = null;
                    _listEnumerator = sequence.List.GetEnumerator();
                }

                _index = -1;
            }

            public DataNode Current => _array == null ? _listEnumerator.Current : _array[_index];
            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_array == null)
                    return _listEnumerator.MoveNext();

                _index++;
                return _index < _array.Length;
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }

            public void Dispose()
            {
                _listEnumerator.Dispose();
            }
        }
    }
}
