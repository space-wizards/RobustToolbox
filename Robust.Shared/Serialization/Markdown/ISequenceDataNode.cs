using System.Collections.Generic;

namespace Robust.Shared.Serialization.Markdown
{
    public interface ISequenceDataNode : IDataNode
    {
        public abstract IReadOnlyList<IDataNode> Sequence { get; }
    }
}
