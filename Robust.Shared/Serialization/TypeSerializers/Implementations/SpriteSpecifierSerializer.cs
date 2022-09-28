using System;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations
{
    public abstract class SpriteSpecifierSerializer :
        ITypeSerializer<Texture, ValueDataNode>,
        ITypeSerializer<EntityPrototype, ValueDataNode>,
        ITypeSerializer<Rsi, MappingDataNode>,
        ITypeSerializer<SpriteSpecifier, MappingDataNode>,
        ITypeSerializer<SpriteSpecifier, ValueDataNode>
    {
        Texture ITypeReader<Texture, ValueDataNode>.Read(ISerializationManager serializationManager,
            ValueDataNode node,
            IDependencyCollection dependencies,
            bool skipHook, ISerializationContext? context, Texture? value)
        {
            var path = serializationManager.Read<ResourcePath>(node, context, skipHook);
            return new Texture(path);
        }

        SpriteSpecifier ITypeReader<SpriteSpecifier, ValueDataNode>.Read(ISerializationManager serializationManager,
            ValueDataNode node,
            IDependencyCollection dependencies,
            bool skipHook, ISerializationContext? context, SpriteSpecifier? value)
        {
            return ((ITypeReader<Texture, ValueDataNode>)this).Read(serializationManager, node, dependencies, skipHook, context, (Texture?)value);
        }

        EntityPrototype ITypeReader<EntityPrototype, ValueDataNode>.Read(ISerializationManager serializationManager,
            ValueDataNode node,
            IDependencyCollection dependencies,
            bool skipHook, ISerializationContext? context, EntityPrototype? value)
        {
            return new EntityPrototype(node.Value);
        }

        Rsi ITypeReader<Rsi, MappingDataNode>.Read(ISerializationManager serializationManager,
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

            var path = serializationManager.Read<ResourcePath>(pathNode, context, skipHook);
            return new Rsi(path, valueDataNode.Value);
        }

        SpriteSpecifier ITypeReader<SpriteSpecifier, MappingDataNode>.Read(ISerializationManager serializationManager,
            MappingDataNode node,
            IDependencyCollection dependencies,
            bool skipHook, ISerializationContext? context, SpriteSpecifier? value)
        {
            if (node.TryGet("entity", out var entityNode) && entityNode is ValueDataNode entityValueNode)
                return ((ITypeReader<EntityPrototype, ValueDataNode>)this).Read(serializationManager, entityValueNode, dependencies, skipHook, context, (EntityPrototype?)value);

            return ((ITypeReader<Rsi, MappingDataNode>) this).Read(serializationManager, node, dependencies, skipHook, context, (Rsi?) value);
        }

        ValidationNode ITypeValidator<SpriteSpecifier, ValueDataNode>.Validate(ISerializationManager serializationManager,
            ValueDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context)
        {
            return ((ITypeReader<Texture, ValueDataNode>) this).Validate(serializationManager, node, dependencies, context);
        }

        ValidationNode ITypeValidator<EntityPrototype, ValueDataNode>.Validate(ISerializationManager serializationManager,
            ValueDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context)
        {
            return !dependencies.Resolve<Prototypes.IPrototypeManager>().HasIndex<Prototypes.EntityPrototype>(node.Value)
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
            if (node.TryGet("entity", out var entityNode))
            {
                if (entityNode is ValueDataNode entityValueNode)
                    return ((ITypeReader<EntityPrototype, ValueDataNode>)this).Validate(serializationManager, entityValueNode, dependencies, context);
                else
                    return new ErrorNode(node, "Sprite specifier entity node must be a ValueDataNode");
            }

            return ((ITypeReader<Rsi, MappingDataNode>) this).Validate(serializationManager, node, dependencies, context);
        }

        ValidationNode ITypeValidator<Rsi, MappingDataNode>.Validate(ISerializationManager serializationManager,
            MappingDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context)
        {
            // apparently explicit interface implementations can't be abstract.
            return ValidateRsi(serializationManager, node, dependencies, context);
        }

        public abstract ValidationNode ValidateRsi(ISerializationManager serializationManager,
            MappingDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context);

        public DataNode Write(ISerializationManager serializationManager, Texture value,
            IDependencyCollection dependencies, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return serializationManager.WriteValue(value.TexturePath, alwaysWrite, context);
        }

        public DataNode Write(ISerializationManager serializationManager, EntityPrototype value,
            IDependencyCollection dependencies, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var mapping = new MappingDataNode();
            mapping.Add("entity", new ValueDataNode(value.EntityPrototypeId));
            return mapping;
        }

        public DataNode Write(ISerializationManager serializationManager, Rsi value, IDependencyCollection dependencies,
            bool alwaysWrite = false,
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

        public DataNode Write(ISerializationManager serializationManager, SpriteSpecifier value,
            IDependencyCollection dependencies, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return value switch
            {
                Rsi rsi
                    => Write(serializationManager, rsi, dependencies, alwaysWrite, context),

                Texture texture
                    => Write(serializationManager, texture, dependencies, alwaysWrite, context),

                EntityPrototype entityPrototype
                    => Write(serializationManager, entityPrototype, dependencies, alwaysWrite, context),

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
