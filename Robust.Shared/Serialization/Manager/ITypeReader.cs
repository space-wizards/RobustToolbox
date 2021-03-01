using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;

namespace Robust.Shared.Serialization.Manager
{
    public interface ITypeReader<TType, TNode> where TType : notnull where TNode : DataNode
    {
        DeserializationResult Read(ISerializationManager serializationManager, TNode node,
            ISerializationContext? context = null);

        ValidatedNode Validate(ISerializationManager serializationManager, TNode node,
            ISerializationContext? context = null);
    }
}
