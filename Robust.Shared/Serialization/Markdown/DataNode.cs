using System;
using System.IO;
using System.Runtime.CompilerServices;
using Robust.Shared.Serialization.Manager;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization.Markdown
{
    public abstract class DataNode
    {
        private const uint InvalidPackedMark = uint.MaxValue;
        private const ulong OverflowPackedMarks = ((ulong) InvalidPackedMark << 32) | 0xFFFFC000;

        // Reserve uint.MaxValue for NodeMark.Invalid. This leaves room for source files with up to 262,143 lines
        // and columns up to 16,383 without taking the slow ahh conditionalweaktable.
        private const int PackedColumnBits = 14;
        private const int MaxPackedLine = (1 << (sizeof(uint) * 8 - PackedColumnBits)) - 2;
        private const int MaxPackedColumn = (1 << PackedColumnBits) - 1;

        // Only really long marks (typically map files) need this.
        // We need to store this as we can't rely on _marks.
        private static readonly ConditionalWeakTable<DataNode, OverflowMarks> OverflowMarksTable = new();

        public string? Tag;

        // Compress the (Start, End) NodeMark into 1 ulong.
        private ulong _marks;

        /// <summary>
        /// Source start location. The common 18-bit line / 14-bit column values are packed with <see cref="End"/> into
        /// <see cref="_marks"/>; exceptionally large locations use the weakref table.
        /// </summary>
        public NodeMark Start
        {
            get => GetMark(start: true);
            set => SetMarks(value, GetMark(start: false));
        }

        /// <summary>
        /// Source end location. See <see cref="Start"/>.
        /// </summary>
        public NodeMark End
        {
            get => GetMark(start: false);
            set => SetMarks(GetMark(start: true), value);
        }

        public DataNode(NodeMark start, NodeMark end)
        {
            SetMarks(start, end);
        }

        private NodeMark GetMark(bool start)
        {
            if (_marks == OverflowPackedMarks)
            {
                if (!OverflowMarksTable.TryGetValue(this, out var marks))
                    throw new InvalidOperationException("Missing overflow source marks");

                return start ? marks.Start : marks.End;
            }

            var packedMark = (uint) (start ? _marks >> 32 : _marks);
            if (packedMark == InvalidPackedMark)
                return NodeMark.Invalid;

            return new NodeMark((int) (packedMark >> PackedColumnBits), (int) (packedMark & MaxPackedColumn));
        }

        private void SetMarks(NodeMark start, NodeMark end)
        {
            var wasOverflow = _marks == OverflowPackedMarks;
            if (TryPackMark(start, out var packedStart) && TryPackMark(end, out var packedEnd))
            {
                _marks = ((ulong) packedStart << 32) | packedEnd;
                if (wasOverflow)
                    OverflowMarksTable.Remove(this);

                return;
            }

            _marks = OverflowPackedMarks;
            if (wasOverflow)
                OverflowMarksTable.Remove(this);

            OverflowMarksTable.Add(this, new OverflowMarks(start, end));
        }

        private static bool TryPackMark(NodeMark mark, out uint packedMark)
        {
            if (mark.Line == -1 && mark.Column == -1)
            {
                packedMark = InvalidPackedMark;
                return true;
            }

            if (mark.Line is < 0 or > MaxPackedLine || mark.Column is < 0 or > MaxPackedColumn)
            {
                packedMark = default;
                return false;
            }

            packedMark = ((uint) mark.Line << PackedColumnBits) | (uint) mark.Column;
            return true;
        }

        private sealed class OverflowMarks(NodeMark start, NodeMark end)
        {
            public readonly NodeMark Start = start;
            public readonly NodeMark End = end;
        }

        public abstract bool IsEmpty { get; }
        public virtual bool IsNull { get; init; } = false;

        public abstract DataNode Copy();

        /// <summary>
        ///     This function will return a data node that contains only the elements within this data node that do not
        ///     have an equivalent entry in some other data node.
        /// </summary>
        public abstract DataNode? Except(DataNode node);

        [Obsolete("Use SerializationManager.PushComposition()")]
        public abstract DataNode PushInheritance(DataNode parent);

        public T CopyCast<T>() where T : DataNode
        {
            return (T) Copy();
        }

        public void Write(TextWriter writer)
        {
            var yaml = this.ToYamlNode();
            var stream = new YamlStream { new(yaml) };
            stream.Save(new YamlMappingFix(new Emitter(writer)), false);
        }

        public override string ToString()
        {
            StringWriter sw = new StringWriter();
            Write(sw);
            return sw.ToString();
        }
    }

    public abstract class DataNode<T> : DataNode where T : DataNode<T>
    {
        protected DataNode(NodeMark start, NodeMark end) : base(start, end)
        {
        }

        public abstract override T Copy();

        public abstract T? Except(T node);

        [Obsolete("Use SerializationManager.PushComposition()")]
        public abstract T PushInheritance(T node);

        public override DataNode? Except(DataNode node)
        {
            return node is not T tNode ? throw new InvalidNodeTypeException() : Except(tNode);
        }

        [Obsolete("Use SerializationManager.PushComposition()")]
        public override DataNode PushInheritance(DataNode parent)
        {
            return parent is not T tNode ? throw new InvalidNodeTypeException() : PushInheritance(tNode);
        }
    }
}
