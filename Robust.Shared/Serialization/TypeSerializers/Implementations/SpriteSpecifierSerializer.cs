using System;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations
{
    [TypeSerializer]
    public sealed class SpriteSpecifierSerializer :
        ITypeSerializer<Texture, ValueDataNode>,
        ITypeSerializer<EntityPrototype, ValueDataNode>,
        ITypeSerializer<Rsi, MappingDataNode>,
        ITypeSerializer<SpriteSpecifier, MappingDataNode>,
        ITypeSerializer<SpriteSpecifier, ValueDataNode>
    {
        DeserializationResult ITypeReader<Texture, ValueDataNode>.Read(ISerializationManager serializationManager,
            ValueDataNode node,
            IDependencyCollection dependencies,
            bool skipHook, ISerializationContext? context, Texture? value)
        {
            var path = serializationManager.ReadValueOrThrow<ResourcePath>(node, context, skipHook);
            return new DeserializedValue<Texture>(new Texture(path));
        }

        DeserializationResult ITypeReader<SpriteSpecifier, ValueDataNode>.Read(
            ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies,
            bool skipHook, ISerializationContext? context, SpriteSpecifier? value)
        {
            try
            {
                return ((ITypeReader<Texture, ValueDataNode>) this).Read(serializationManager, node, dependencies, skipHook, context, (Texture?)value);
            }
            catch { /* ignored */ }

            try
            {
                return ((ITypeReader<EntityPrototype, ValueDataNode>) this).Read(serializationManager, node, dependencies, skipHook, context, (EntityPrototype?) value);
            }
            catch { /* ignored */ }

            throw new InvalidMappingException(
                "SpriteSpecifier was neither a Texture nor an EntityPrototype but got provided a ValueDataNode");
        }

        DeserializationResult ITypeReader<EntityPrototype, ValueDataNode>.Read(
            ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies,
            bool skipHook, ISerializationContext? context, EntityPrototype? value)
        {
            return new DeserializedValue<EntityPrototype>(new EntityPrototype(node.Value));
        }

        DeserializationResult ITypeReader<Rsi, MappingDataNode>.Read(ISerializationManager serializationManager,
            MappingDataNode node,
            IDependencyCollection dependencies,
            bool skipHook, ISerializationContext? context, Rsi? value)
        {
            if (!node.TryGet("sprite", out var pathNode))
            {
                throw new InvalidMappingException("Expected sprite-node");
            }

            if (!node.TryGet("state", out var stateNode) || stateNode is not ValueDataNode valueDataNode)
            {
                throw new InvalidMappingException("Expected state-node as a valuenode");
            }

            var path = serializationManager.ReadValueOrThrow<ResourcePath>(pathNode, context, skipHook);
            return new DeserializedValue<Rsi>(new Rsi(path, valueDataNode.Value));
        }


        DeserializationResult ITypeReader<SpriteSpecifier, MappingDataNode>.Read(
            ISerializationManager serializationManager, MappingDataNode node,
            IDependencyCollection dependencies,
            bool skipHook, ISerializationContext? context, SpriteSpecifier? value)
        {
            return ((ITypeReader<Rsi, MappingDataNode>) this).Read(serializationManager, node, dependencies, skipHook, context, (Rsi?) value);
        }

        ValidationNode ITypeValidator<SpriteSpecifier, ValueDataNode>.Validate(ISerializationManager serializationManager,
            ValueDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context)
        {
            var texNode = ((ITypeReader<Texture, ValueDataNode>) this).Validate(serializationManager, node, dependencies, context);
            if (texNode is ErrorNode) return texNode;

            var protNode = ((ITypeReader<EntityPrototype, ValueDataNode>) this).Validate(serializationManager, node, dependencies, context);
            if (protNode is ErrorNode) return texNode;

            return new ValidatedValueNode(node);
        }

        ValidationNode ITypeValidator<EntityPrototype, ValueDataNode>.Validate(ISerializationManager serializationManager,
            ValueDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context)
        {
            // TODO Serialization: actually validate the id
            return string.IsNullOrWhiteSpace(node.Value)
                ? new ErrorNode(node, $"Invalid {nameof(EntityPrototype)} id")
                : new ValidatedValueNode(node);
        }


        ValidationNode ITypeValidator<Texture, ValueDataNode>.Validate(ISerializationManager serializationManager,
            ValueDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context)
        {
            return serializationManager.ValidateNode(typeof(ResourcePath), new ValueDataNode($"{SharedSpriteComponent.TextureRoot / node.Value}"), context);
        }

        ValidationNode ITypeValidator<SpriteSpecifier, MappingDataNode>.Validate(
            ISerializationManager serializationManager, MappingDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context)
        {
            return ((ITypeReader<Rsi, MappingDataNode>) this).Validate(serializationManager, node, dependencies, context);
        }

        ValidationNode ITypeValidator<Rsi, MappingDataNode>.Validate(ISerializationManager serializationManager,
            MappingDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context)
        {
            if (!node.TryGet("sprite", out var pathNode) || pathNode is not ValueDataNode valuePathNode)
            {
                return new ErrorNode(node, "Missing/Invalid sprite node");
            }

            if (!node.TryGet("state", out var stateNode) || stateNode is not ValueDataNode)
            {
                return new ErrorNode(node, "Missing/Invalid state node");
            }

            var path = serializationManager.ValidateNode(typeof(ResourcePath),
                new ValueDataNode($"{SharedSpriteComponent.TextureRoot / valuePathNode.Value}"), context);

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
            mapping.Add("sprite", serializationManager.WriteValue(value.RsiPath));
            mapping.Add("state", new ValueDataNode(value.RsiState));
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

        public DataNode Write(ISerializationManager serializationManager, SpriteSpecifier value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return value switch
            {
                Rsi rsi
                    => Write(serializationManager, rsi, alwaysWrite, context),

                Texture texture
                    => Write(serializationManager, texture, alwaysWrite, context),

                EntityPrototype entityPrototype
                    => Write(serializationManager, entityPrototype, alwaysWrite, context),

                _ => throw new InvalidOperationException("Invalid SpriteSpecifier specified!")
            };
        }

        public SpriteSpecifier Copy(ISerializationManager serializationManager, SpriteSpecifier source, SpriteSpecifier target,
            bool skipHook, ISerializationContext? context = null)
        {
            return source switch
            {
                Rsi rsi
                    => new Rsi(rsi.RsiPath, rsi.RsiState),

                Texture texture
                    => new Texture(texture.TexturePath),

                EntityPrototype entityPrototype
                    => new EntityPrototype(entityPrototype.EntityPrototypeId),

                _ => throw new InvalidOperationException("Invalid SpriteSpecifier specified!")
            };
        }
    }
}
