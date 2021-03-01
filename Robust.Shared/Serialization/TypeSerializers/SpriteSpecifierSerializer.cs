using System;
using JetBrains.Annotations;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Robust.Shared.Serialization.TypeSerializers
{
    [TypeSerializer]
    public class SpriteSpecifierSerializer :
        ITypeReaderWriter<SpriteSpecifier, ValueDataNode>,
        ITypeReaderWriter<SpriteSpecifier, MappingDataNode>,
        ITypeCopier<Rsi>,
        ITypeCopier<Texture>,
        ITypeCopier<EntityPrototype>
    {
        public DeserializationResult Read(ISerializationManager serializationManager,
            ValueDataNode node, ISerializationContext? context = null)
        {
            var path = serializationManager.ReadValueOrThrow<ResourcePath>(node, context);
            var texture = new Texture(path);

            return new DeserializedValue<SpriteSpecifier>(texture);
        }

        public ValidatedNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            ISerializationContext? context = null)
        {
            return serializationManager.ReadValue<ResourcePath>(node) != null ? new ValidatedValueNode(node) : new ErrorNode(node);
        }

        public DeserializationResult Read(ISerializationManager serializationManager,
            MappingDataNode node, ISerializationContext? context = null)
        {
            if (node.TryGetNode("sprite", out var spriteNode)
                && node.TryGetNode("state", out var rawStateNode)
                && rawStateNode is ValueDataNode stateNode)
            {
                var path = serializationManager.ReadValueOrThrow<ResourcePath>(spriteNode, context);
                var rsi = new Rsi(path, stateNode.Value);

                return new DeserializedValue<SpriteSpecifier>(rsi);
            }

            throw new InvalidNodeTypeException();
        }

        public ValidatedNode Validate(ISerializationManager serializationManager, MappingDataNode node,
            ISerializationContext? context = null)
        {
            return node.HasNode("sprite") &&
                   node.TryGetNode("state", out var stateNode) &&
                   stateNode is ValueDataNode &&
                   serializationManager.ReadValue<ResourcePath>(stateNode) != null
                ? new ValidatedValueNode(node)
                : new ErrorNode(node);
        }

        public DataNode Write(ISerializationManager serializationManager, SpriteSpecifier value,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            switch (value)
            {
                case Texture tex:
                    return serializationManager.WriteValue(tex.TexturePath, alwaysWrite, context);
                case Rsi rsi:
                    var mapping = new MappingDataNode();
                    mapping.AddNode("sprite", serializationManager.WriteValue(rsi.RsiPath, alwaysWrite, context));
                    mapping.AddNode("state", new ValueDataNode(rsi.RsiState));
                    return mapping;
            }

            throw new NotImplementedException();
        }

        public Rsi Copy(ISerializationManager serializationManager, Rsi source, Rsi target, ISerializationContext? context = null)
        {
            return new(source.RsiPath, source.RsiState);
        }

        public Texture Copy(ISerializationManager serializationManager, Texture source, Texture target, ISerializationContext? context = null)
        {
            return new(source.TexturePath);
        }

        [MustUseReturnValue]
        public EntityPrototype Copy(ISerializationManager serializationManager, EntityPrototype source,
            EntityPrototype target, ISerializationContext? context = null)
        {
            return new(source.EntityPrototypeId);
        }
    }
}
