using System;
using System.Linq;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations;

[TypeSerializer]
public sealed class ResPathSerializer : ITypeSerializer<ResPath, ValueDataNode>, ITypeCopyCreator<ResPath>
{
    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        var path = ResPath.FromRelativeSystemPath(node.Value);

        if (path.Extension.Equals("rsi"))
        {
            path /= "meta.json";
        }

        if (!path.CanonPath.Split('/').First().Equals("Textures", StringComparison.InvariantCultureIgnoreCase))
        {
            path = SpriteSpecifierSerializer.TextureRoot / path;
        }

        path = path.ToRootedPath();

        try
        {
            var resourceManager = dependencies.Resolve<IResourceManager>();
            if (node.Value.EndsWith(ResPath.Separator))
            {
                if (resourceManager.ContentGetDirectoryEntries(path).Any())
                    return new ValidatedValueNode(node);

                return new ErrorNode(node, $"Folder not found. ({path})");
            }

            if (resourceManager.ContentFileExists(path))
                return new ValidatedValueNode(node);

            return new ErrorNode(node, $"File not found. ({path})");
        }
        catch (Exception e)
        {
            return new ErrorNode(node, $"Failed parsing filepath. ({path}) ({e.Message})");
        }
    }

    public ResPath Read(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx, ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<ResPath>? instanceProvider = null)
    {
        return new ResPath(node.Value);
    }

    public DataNode Write(ISerializationManager serializationManager, ResPath value, IDependencyCollection dependencies,
        bool alwaysWrite = false, ISerializationContext? context = null)
    {
        return new ValueDataNode(value.ToString());
    }

    public ResPath CreateCopy(ISerializationManager serializationManager, ResPath source,
        IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
    {
        return new ResPath(source.ToString());
    }
}
