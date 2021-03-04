using Robust.Shared.GameObjects;
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
        ITypeSerializer<Texture, ValueDataNode>,
        ITypeSerializer<EntityPrototype, ValueDataNode>,
        ITypeSerializer<Rsi, MappingDataNode>,
        ITypeReader<SpriteSpecifier, MappingDataNode>,
        ITypeReader<SpriteSpecifier, ValueDataNode>
    {
        DeserializationResult ITypeReader<Texture, ValueDataNode>.Read(ISerializationManager serializationManager, ValueDataNode node,
            bool skipHook, ISerializationContext? context)
        {
            var path = serializationManager.ReadValueOrThrow<ResourcePath>(node, context, skipHook);
            return DeserializationResult.Value(new Texture(path));
        }

        DeserializationResult ITypeReader<SpriteSpecifier, ValueDataNode>.Read(ISerializationManager serializationManager, ValueDataNode node,
            bool skipHook, ISerializationContext? context)
        {
            try
            {
                return ((ITypeReader<Texture, ValueDataNode>) this).Read(serializationManager, node, skipHook, context);
            }
            catch { /* ignored */ }

            try
            {
                return ((ITypeReader<EntityPrototype, ValueDataNode>) this).Read(serializationManager, node, skipHook, context);
            }
            catch { /* ignored */ }

            throw new InvalidMappingException(
                "SpriteSpecifier was neither a Texture nor an EntityPrototype but got provided a valuedatanode");
        }

        DeserializationResult ITypeReader<EntityPrototype, ValueDataNode>.Read(ISerializationManager serializationManager, ValueDataNode node,
            bool skipHook, ISerializationContext? context)
        {
            return DeserializationResult.Value(new EntityPrototype(node.Value));
        }

        DeserializationResult ITypeReader<Rsi, MappingDataNode>.Read(ISerializationManager serializationManager, MappingDataNode node,
            bool skipHook, ISerializationContext? context)
        {
            if (!node.TryGetNode("sprite", out var pathNode))
            {
                throw new InvalidMappingException("Expected sprite-node");
            }

            if (!node.TryGetNode("state", out var stateNode) || stateNode is not ValueDataNode valueDataNode)
            {
                throw new InvalidMappingException("Expected state-node as a valuenode");
            }

            var path = serializationManager.ReadValueOrThrow<ResourcePath>(pathNode, context, skipHook);
            return DeserializationResult.Value(new Rsi(path, valueDataNode.Value));
        }


        DeserializationResult ITypeReader<SpriteSpecifier, MappingDataNode>.Read(ISerializationManager serializationManager, MappingDataNode node,
            bool skipHook, ISerializationContext? context)
        {
            return ((ITypeReader<Rsi, MappingDataNode>) this).Read(serializationManager, node, skipHook, context);
        }

        ValidationNode ITypeReader<SpriteSpecifier, ValueDataNode>.Validate(ISerializationManager serializationManager, ValueDataNode node,
            ISerializationContext? context)
        {
            var texNode = ((ITypeReader<Texture, ValueDataNode>) this).Validate(serializationManager, node, context);
            if (texNode is ErrorNode) return texNode;

            var protNode = ((ITypeReader<EntityPrototype, ValueDataNode>) this).Validate(serializationManager, node, context);
            if (protNode is ErrorNode) return texNode;

            return new ValidatedValueNode(node);
        }

        ValidationNode ITypeReader<EntityPrototype, ValueDataNode>.Validate(ISerializationManager serializationManager, ValueDataNode node,
            ISerializationContext? context)
        {
            //todo paul actually validate the id
            return string.IsNullOrWhiteSpace(node.Value) ? new ErrorNode(node, "Invalid entityprototypeid", true) : new ValidatedValueNode(node);
        }


        ValidationNode ITypeReader<Texture, ValueDataNode>.Validate(ISerializationManager serializationManager, ValueDataNode node,
            ISerializationContext? context)
        {
            return serializationManager.ValidateNode(typeof(ResourcePath), new ValueDataNode($"{SharedSpriteComponent.TextureRoot / node.Value}"), context);
        }

        ValidationNode ITypeReader<SpriteSpecifier, MappingDataNode>.Validate(ISerializationManager serializationManager, MappingDataNode node,
            ISerializationContext? context)
        {
            return ((ITypeReader<Rsi, MappingDataNode>) this).Validate(serializationManager, node, context);
        }

        ValidationNode ITypeReader<Rsi, MappingDataNode>.Validate(ISerializationManager serializationManager, MappingDataNode node,
            ISerializationContext? context)
        {
            if (!node.TryGetNode("sprite", out var pathNode) || pathNode is not ValueDataNode valuePathNode)
            {
                return new ErrorNode(node, "Missing/Invalid spritenode", true);
            }

            if (!node.TryGetNode("state", out var stateNode) || stateNode is not ValueDataNode)
            {
                return new ErrorNode(node, "Missing/Invalid statenode", true);
            }

            var path = serializationManager.ValidateNode(typeof(ResourcePath), new ValueDataNode($"{SharedSpriteComponent.TextureRoot / valuePathNode.Value}"), context);

            if (path is ErrorNode) return path;

            return new ValidatedValueNode(node);
        }

        public DataNode Write(ISerializationManager serializationManager, Texture value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return serializationManager.WriteValue(value.TexturePath, alwaysWrite, context);
        }

        public DataNode Write(ISerializationManager serializationManager, EntityPrototype value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.EntityPrototypeId);
        }


        public DataNode Write(ISerializationManager serializationManager, Rsi value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var mapping = new MappingDataNode();
            mapping.AddNode("sprite", serializationManager.WriteValue(value.RsiPath));
            mapping.AddNode("state", new ValueDataNode(value.RsiState));
            return mapping;
        }

        public Texture Copy(ISerializationManager serializationManager, Texture source, Texture target, bool skipHook,
            ISerializationContext? context = null)
        {
            return new(source.TexturePath);
        }

        public EntityPrototype Copy(ISerializationManager serializationManager, EntityPrototype source, EntityPrototype target,
            bool skipHook, ISerializationContext? context = null)
        {
            return new(source.EntityPrototypeId);
        }

        public Rsi Copy(ISerializationManager serializationManager, Rsi source, Rsi target, bool skipHook,
            ISerializationContext? context = null)
        {
            return new(source.RsiPath, source.RsiState);
        }
    }
}
