using System;
using YamlDotNet.Core;

namespace Robust.Shared.Serialization.Markdown
{
    public readonly struct NodeMark : IEquatable<NodeMark>, IComparable<NodeMark>
    {
        public static NodeMark Invalid => new(-1, -1);

        public NodeMark(int line, int column)
        {
            Line = line;
            Column = column;
        }

        public NodeMark(Mark mark) : this(mark.Line, mark.Column)
        {
        }

        public int Line { get; init; }
        public int Column { get; init; }

        public override string ToString()
        {
            return $"Line: {Line}, Col: {Column}";
        }

        public bool Equals(NodeMark other)
        {
            return Line == other.Line && Column == other.Column;
        }

        public override bool Equals(object? obj)
        {
            return obj is NodeMark other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Line, Column);
        }

        public int CompareTo(NodeMark other)
        {
            var lineNum = Line.CompareTo(other.Line);
            return lineNum == 0 ? Column.CompareTo(other.Column) : lineNum;
        }

        public static implicit operator NodeMark(Mark mark) => new(mark);

        public static bool operator ==(NodeMark left, NodeMark right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(NodeMark left, NodeMark right)
        {
            return !left.Equals(right);
        }

        public static bool operator <(NodeMark? left, NodeMark? right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            return left.Value.CompareTo(right.Value) < 0;
        }

        public static bool operator >(NodeMark? left, NodeMark? right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            return left.Value.CompareTo(right.Value) > 0;
        }
    }
}
