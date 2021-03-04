using System;
using YamlDotNet.Core;

namespace Robust.Shared.Serialization.Markdown
{
    public struct DataPosition
    {
        public readonly int Line;
        public readonly int Column;

        public DataPosition(int line, int column)
        {
            Line = line;
            Column = column;
        }

        public DataPosition(Mark mark)
        {
            Line = mark.Line;
            Column = mark.Column;
        }

        public override int GetHashCode()
        {
            var code = new HashCode();
            code.Add(Line.GetHashCode());
            code.Add(Column.GetHashCode());
            return code.ToHashCode();
        }

        public static DataPosition Invalid => new DataPosition(-1, -1);

        public static implicit operator DataPosition(Mark mark) => new DataPosition(mark);
    }
}
