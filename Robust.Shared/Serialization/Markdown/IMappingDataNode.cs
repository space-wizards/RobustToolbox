using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Robust.Shared.Serialization.Markdown
{
    public interface IMappingDataNode : IDataNode
    {
        public abstract bool TryGetNode(string key, [NotNullWhen(true)] out IDataNode? node);
        public abstract bool HasNode(string key);
    }
}
