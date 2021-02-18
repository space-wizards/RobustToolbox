using System;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.TypeSerializers
{
    [TypeSerializer]
    public class SpriteSpecifierSerializer :
        ITypeSerializer<SpriteSpecifier, ValueDataNode>,
        ITypeSerializer<SpriteSpecifier, MappingDataNode>
    {
        [Dependency] private readonly IServ3Manager _serv3Manager = default!;

        public SpriteSpecifier Read(ValueDataNode node, ISerializationContext? context = null)
        {
            return new SpriteSpecifier.Texture(_serv3Manager.ReadValue<ResourcePath>(node, context));
        }

        public SpriteSpecifier Read(MappingDataNode node, ISerializationContext? context = null)
        {
            if (node.TryGetNode("sprite", out var spriteNode)
                && node.TryGetNode("state", out var rawStateNode)
                && rawStateNode is ValueDataNode stateNode)
            {
                return new SpriteSpecifier.Rsi(_serv3Manager.ReadValue<ResourcePath>(spriteNode, context), stateNode.Value);
            }

            throw new InvalidNodeTypeException();
        }

        public DataNode Write(SpriteSpecifier value,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            switch (value)
            {
                case SpriteSpecifier.Texture tex:
                    return _serv3Manager.WriteValue(tex.TexturePath, alwaysWrite, context);
                case SpriteSpecifier.Rsi rsi:
                    var mapping = new MappingDataNode();
                    mapping.AddNode("sprite", _serv3Manager.WriteValue(rsi.RsiPath, alwaysWrite, context));
                    mapping.AddNode("state", new ValueDataNode(rsi.RsiState));
                    return mapping;
            }
            throw new NotImplementedException();
        }
    }
}
