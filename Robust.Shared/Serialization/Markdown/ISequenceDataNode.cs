using System.Collections.Generic;

namespace Robust.Shared.Serialization.Markdown
{
    public interface ISequenceDataNode : IDataNode
    {
        IReadOnlyList<IDataNode> Sequence { get; }
        void Add(IDataNode node);
    }
}
