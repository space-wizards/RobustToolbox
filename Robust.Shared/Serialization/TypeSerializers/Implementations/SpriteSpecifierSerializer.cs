using System;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
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
        ITypeSerializer<SpriteSpecifier, ValueDataNode>,
        ITypeCopyCreator<SpriteSpecifier>,
        ITypeCopyCreator<Rsi>,
        ITypeCopyCreator<Texture>,
        ITypeCopyCreator<EntityPrototype>,
        ITypeCopier<Rsi>,
        ITypeCopier<Texture>
    {
        // Should probably be in SpriteComponent, but is needed for server to validate paths.
        // So I guess it might as well go here?
        public static readonly ResPath TextureRoot = new("/Textures");

        Texture ITypeReader<Texture, ValueDataNode>.Read(ISerializationManager serializationManager,
            ValueDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx, ISerializationContext? context,
            ISerializationManager.InstantiationDelegate<Texture>? instanceProvider)
        {
            var path = serializationManager.Read<ResPath>(node, hookCtx, context);
            return new Texture(path);
        }

        SpriteSpecifier ITypeReader<SpriteSpecifier, ValueDataNode>.Read(ISerializationManager serializationManager,
            ValueDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx, ISerializationContext? context,
            ISerializationManager.InstantiationDelegate<SpriteSpecifier>? instanceProvider)
        {
            return ((ITypeReader<Texture, ValueDataNode>)this).Read(serializationManager, node, dependencies, hookCtx, context, (ISerializationManager.InstantiationDelegate<Texture>?)instanceProvider);
        }

        EntityPrototype ITypeReader<EntityPrototype, ValueDataNode>.Read(ISerializationManager serializationManager,
            ValueDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx, ISerializationContext? context,
            ISerializationManager.InstantiationDelegate<EntityPrototype>? instanceProvider)
        {
            return new EntityPrototype(node.Value);
        }

        Rsi ITypeReader<Rsi, MappingDataNode>.Read(ISerializationManager serializationManager,
            MappingDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx, ISerializationContext? context,
            ISerializationManager.InstantiationDelegate<Rsi>? instanceProvider)
        {
            if (!node.TryGet("sprite", out var pathNode))
            {
                throw new InvalidMappingException("Expected sprite-node");
            }

            if (!node.TryGet("state", out var stateNode) || stateNode is not ValueDataNode valueDataNode)
            {
                throw new InvalidMappingException("Expected state-node as a valuenode");
            }

            var path = serializationManager.Read<ResPath>(pathNode, hookCtx, context);
            return new Rsi(path, valueDataNode.Value);
        }

        SpriteSpecifier ITypeReader<SpriteSpecifier, MappingDataNode>.Read(ISerializationManager serializationManager,
            MappingDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx, ISerializationContext? context,
            ISerializationManager.InstantiationDelegate<SpriteSpecifier>? instanceProvider)
        {
            if (node.TryGet("entity", out var entityNode) && entityNode is ValueDataNode entityValueNode)
                return ((ITypeReader<EntityPrototype, ValueDataNode>)this).Read(serializationManager, entityValueNode, dependencies, hookCtx, context, (ISerializationManager.InstantiationDelegate<EntityPrototype>?)instanceProvider);

            return ((ITypeReader<Rsi, MappingDataNode>) this).Read(serializationManager, node, dependencies, hookCtx, context, (ISerializationManager.InstantiationDelegate<Rsi>?)instanceProvider);
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
            return serializationManager.ValidateNode<ResPath>(new ValueDataNode($"{TextureRoot / node.Value}"), context);
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

        public Texture CreateCopy(ISerializationManager serializationManager, Texture source,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
        {
            return new(source.TexturePath);
        }

        public EntityPrototype CreateCopy(ISerializationManager serializationManager, EntityPrototype source,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
        {
            return new(source.EntityPrototypeId);
        }

        public Rsi CreateCopy(ISerializationManager serializationManager, Rsi source,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
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

        public SpriteSpecifier CreateCopy(ISerializationManager serializationManager, SpriteSpecifier source,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
        {
            return source switch
            {
                Rsi rsi
                    => CreateCopy(serializationManager, rsi, dependencies, hookCtx, context),

                Texture texture
                    => CreateCopy(serializationManager, texture, dependencies, hookCtx, context),

                EntityPrototype entityPrototype
                    => CreateCopy(serializationManager, entityPrototype, dependencies, hookCtx, context),

                _ => throw new InvalidOperationException("Invalid SpriteSpecifier specified!")
            };
        }

        public void CopyTo(ISerializationManager serializationManager, Rsi source, ref Rsi target,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null)
        {
            target.RsiPath = source.RsiPath;
            target.RsiState = source.RsiState;
        }

        public void CopyTo(ISerializationManager serializationManager, Texture source, ref Texture target,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null)
        {
            target.TexturePath = source.TexturePath;
        }
    }
}
