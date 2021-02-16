using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Robust.Shared.Serialization.Markdown
{
    public interface IMappingDataNode : IDataNode
    {
        IReadOnlyDictionary<IDataNode, IDataNode> Children { get; }
        IDataNode GetNode(IDataNode key);
        IDataNode GetNode(string key);
        bool TryGetNode(IDataNode key, [NotNullWhen(true)] out IDataNode? node);
        bool TryGetNode(string key, [NotNullWhen(true)] out IDataNode? node);
        bool HasNode(IDataNode key);
        bool HasNode(string key);
        void AddNode(IDataNode key, IDataNode node);
        void AddNode(string key, IDataNode node);
        void RemoveNode(IDataNode key);
        void RemoveNode(string key);
    }
}
