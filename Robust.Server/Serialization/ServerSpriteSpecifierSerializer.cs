using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Robust.Server.Serialization;

[TypeSerializer]
public sealed class ServerSpriteSpecifierSerializer : SpriteSpecifierSerializer
{
    public override ValidationNode ValidateRsi(ISerializationManager serializationManager,
    MappingDataNode node,
    IDependencyCollection dependencies,
    ISerializationContext? context)
    {
        if (!node.TryGet("sprite", out var pathNode) || pathNode is not ValueDataNode valuePathNode)
        {
            return new ErrorNode(node, "Sprite specifier has missing/invalid sprite node");
        }

        if (!valuePathNode.Value.EndsWith(".rsi")) // required so that resource path validation checks for the meta.json.
        {
            return new ErrorNode(node, "sprite node does not end in .rsi");
        }

        if (!node.TryGet("state", out var stateNode) || stateNode is not ValueDataNode valueStateNode)
        {
            return new ErrorNode(node, "Sprite specifier has missing/invalid state node");
        }

        var path = serializationManager.ValidateNode(typeof(ResourcePath),
            new ValueDataNode($"{SharedSpriteComponent.TextureRoot / valuePathNode.Value}"), context);

        if (path is ErrorNode) return path;

        // RSI meta-data & misc related functions are client only, so the server can't easily fully validate them.
        // However, as some sprites may be specified in server-exclusive prototypes, we should still try and check that
        // the state exists. So lets just check if the state .png exists, without properly validating the RSI's
        // meta.json

        var statePath = serializationManager.ValidateNode(typeof(ResourcePath),
            new ValueDataNode($"{SharedSpriteComponent.TextureRoot / valuePathNode.Value / valueStateNode.Value}.png"), context);

        if (statePath is ErrorNode) return statePath;

        return new ValidatedValueNode(node);
    }
}
