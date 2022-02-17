using System;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations
{
    [TypeSerializer]
    public sealed class ResourcePathSerializer : ITypeSerializer<ResourcePath, ValueDataNode>
    {
        public DeserializationResult Read(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies,
            bool skipHook,
            ISerializationContext? context = null, ResourcePath? value = default)
        {
            return new DeserializedValue<ResourcePath>(new ResourcePath(node.Value));
        }

        public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null)
        {
            var path = new ResourcePath(node.Value);

            if (path.Extension.Equals("rsi"))
            {
                path /= "meta.json";
            }

            if (!path.EnumerateSegments().First().Equals("Textures", StringComparison.InvariantCultureIgnoreCase))
            {
                path = SharedSpriteComponent.TextureRoot / path;
            }

            path = path.ToRootedPath();

            try
            {
                return IoCManager.Resolve<IResourceManager>().ContentFileExists(path)
                    ? new ValidatedValueNode(node)
                    : new ErrorNode(node, $"File not found. ({path})");
            }
            catch (Exception e)
            {
                return new ErrorNode(node, $"Failed parsing filepath. ({path}) ({e.Message})");
            }
        }

        public DataNode Write(ISerializationManager serializationManager, ResourcePath value,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString());
        }

        [MustUseReturnValue]
        public ResourcePath Copy(ISerializationManager serializationManager, ResourcePath source, ResourcePath target,
            bool skipHook,
            ISerializationContext? context = null)
        {
            return new(source.ToString());
        }
    }
}
