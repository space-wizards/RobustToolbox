using System;

namespace Robust.Shared.Serialization.Markdown
{
    public interface IDataNode : IEquatable<IDataNode>
    {
        IDataNode Copy();
    }
}
