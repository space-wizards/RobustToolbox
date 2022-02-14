using System;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects;

public sealed partial class SpriteSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    [Pure]
    public Texture Frame0(SpriteSpecifier specifier)
    {
        return RsiStateLike(specifier).Default;
    }

    [Pure]
    public IRsiStateLike RsiStateLike(SpriteSpecifier specifier)
    {
        switch (specifier)
        {
            case SpriteSpecifier.Texture tex:
                return tex.GetTexture(_resourceCache);

            case SpriteSpecifier.Rsi rsi:
                return GetState(rsi);

            case SpriteSpecifier.EntityPrototype prototypeIcon:
                if (!_proto.TryIndex<EntityPrototype>(prototypeIcon.EntityPrototypeId, out var prototype))
                {
                    Logger.Error("Failed to load PrototypeIcon {0}", prototypeIcon.EntityPrototypeId);
                    return SpriteComponent.GetFallbackState(_resourceCache);
                }

                return SpriteComponent.GetPrototypeIcon(prototype, _resourceCache);

            default:
                throw new NotSupportedException();
        }
    }

    [Pure]
    public IRsiStateLike GetPrototypeIcon(EntityPrototype prototype, IResourceCache resourceCache)
    {
        var icon = IconComponent.GetPrototypeIcon(prototype, _resourceCache);
        if (icon != null) return icon;

        if (!prototype.Components.ContainsKey("Sprite"))
        {
            return SpriteComponent.GetFallbackState(resourceCache);
        }

        var dummy = Spawn(prototype.ID, MapCoordinates.Nullspace);
        var spriteComponent = EnsureComp<SpriteComponent>(dummy);
        var result = spriteComponent.Icon ?? SpriteComponent.GetFallbackState(resourceCache);
        Del(dummy);

        return result;
    }

    [Pure]
    public RSI.State GetState(SpriteSpecifier.Rsi rsiSpecifier)
    {
        if (_resourceCache.TryGetResource<RSIResource>(
                SharedSpriteComponent.TextureRoot / rsiSpecifier.RsiPath,
                out var theRsi) &&
            theRsi.RSI.TryGetState(rsiSpecifier.RsiState, out var state))
        {
            return state;
        }

        Logger.Error("Failed to load RSI {0}", rsiSpecifier.RsiPath);
        return SpriteComponent.GetFallbackState(_resourceCache);
    }
}
