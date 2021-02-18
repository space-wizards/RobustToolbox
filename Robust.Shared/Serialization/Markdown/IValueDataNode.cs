using System;

namespace Robust.Shared.Serialization.Markdown
{
    public interface IValueDataNode : IDataNode
    {
        string Value { get; }

        public string GetValue();

        bool IEquatable<IDataNode>.Equals(IDataNode? other)
        {
            if (other is not IValueDataNode val) return false;
            return Value == val.Value;
        }
    }
}
