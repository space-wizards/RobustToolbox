using System;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.TypeSerializers
{
    [TypeSerializer]
    public class SpriteSpecifierSerializer : ITypeSerializer<SpriteSpecifier>
    {
        [Dependency] private readonly IServ3Manager _serv3Manager = default!;

        public SpriteSpecifier NodeToType(DataNode node, ISerializationContext? context = null)
        {
            if (node is ValueDataNode valueDataNode)
            {
                return new SpriteSpecifier.Texture(_serv3Manager.ReadValue<ResourcePath>(valueDataNode, context));
            }

            if (node is MappingDataNode mapping
                && mapping.TryGetNode("sprite", out var spriteNode)
                && mapping.TryGetNode("state", out var rawStateNode)
                && rawStateNode is ValueDataNode stateNode)
            {
                return new SpriteSpecifier.Rsi(_serv3Manager.ReadValue<ResourcePath>(spriteNode, context), stateNode.GetValue());
            }
            throw new InvalidNodeTypeException();
        }

        public DataNode TypeToNode(SpriteSpecifier value,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            switch (value)
            {
                case SpriteSpecifier.Texture tex:
                    return _serv3Manager.WriteValue(tex.TexturePath, nodeFactory, alwaysWrite, context);
                case SpriteSpecifier.Rsi rsi:
                    var mapping = nodeFactory.GetMappingNode();
                    mapping.AddNode("sprite", _serv3Manager.WriteValue(rsi.RsiPath, nodeFactory, alwaysWrite, context));
                    mapping.AddNode("state", new ValueDataNode(rsi.RsiState));
                    return mapping;
            }
            throw new NotImplementedException();
        }
    }
}
