using System;
using System.Collections.Generic;
using YamlDotNet.Core.Tokens;

namespace Robust.Shared.Serialization.Markdown
{
    public interface ISequenceDataNode : IDataNode
    {
        IReadOnlyList<IDataNode> Sequence { get; }
        void Add(IDataNode node);
        void Remove(IDataNode node);
        bool IEquatable<IDataNode>.Equals(IDataNode? other)
        {
            return this == other;
        }
    }
}
