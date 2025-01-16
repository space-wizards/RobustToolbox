using System;
using System.Collections.Generic;
using System.Numerics;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects;

// This partial class contains various public helper methods, including methods for extracting textures/icons from
// sprite specifiers and entity prototypes.
public sealed partial class SpriteSystem
{
    private readonly Dictionary<string, IRsiStateLike> _cachedPrototypeIcons = new();

    public Texture Frame0(EntityPrototype prototype)
    {
        return GetPrototypeIcon(prototype).Default;
    }

    public Texture Frame0(SpriteSpecifier specifier)
    {
        return RsiStateLike(specifier).Default;
    }

    public IRsiStateLike RsiStateLike(SpriteSpecifier specifier)
    {
        switch (specifier)
        {
            case SpriteSpecifier.Texture tex:
                return GetTexture(tex);

            case SpriteSpecifier.Rsi rsi:
                return GetState(rsi);

            case SpriteSpecifier.EntityPrototype prototypeIcon:
                return GetPrototypeIcon(prototypeIcon.EntityPrototypeId);

            default:
                throw new NotSupportedException();
        }
    }

    public Texture GetIcon(IconComponent icon)
    {
        return GetState(icon.Icon).Frame0;
    }

    /// <summary>
    ///     Returns an icon for a given <see cref="EntityPrototype"/> ID, or a fallback in case of an error.
    ///     This method caches the result based on the prototype identifier.
    /// </summary>
    public IRsiStateLike GetPrototypeIcon(string prototype)
    {
        // Check if this prototype has been cached before, and if so return the result.
        if (_cachedPrototypeIcons.TryGetValue(prototype, out var cachedResult))
            return cachedResult;

        if (!_proto.TryIndex<EntityPrototype>(prototype, out var entityPrototype))
        {
            // The specified prototype doesn't exist, return the fallback "error" sprite.
            _sawmill.Error("Failed to load PrototypeIcon {0}", prototype);
            return GetFallbackState();
        }

        // Generate the icon and cache it in case it's ever needed again.
        var result = GetPrototypeIcon(entityPrototype);
        _cachedPrototypeIcons[prototype] = result;

        return result;
    }

    /// <summary>
    ///     Returns an icon for a given <see cref="EntityPrototype"/> ID, or a fallback in case of an error.
    ///     This method does NOT cache the result.
    /// </summary>
    public IRsiStateLike GetPrototypeIcon(EntityPrototype prototype)
    {
        // IconComponent takes precedence. If it has a valid icon, return that. Otherwise, continue as normal.
        if (prototype.Components.TryGetValue("Icon", out var compData)
            && compData.Component is IconComponent icon)
        {
            return GetIcon(icon);
        }

        // If the prototype doesn't have a SpriteComponent, then there's nothing we can do but return the fallback.
        if (!prototype.Components.ContainsKey("Sprite"))
        {
            return GetFallbackState();
        }

        // Finally, we use spawn a dummy entity to get its icon.
        var dummy = Spawn(prototype.ID, MapCoordinates.Nullspace);
        var spriteComponent = EnsureComp<SpriteComponent>(dummy);
        var result = spriteComponent.Icon ?? GetFallbackState();
        Del(dummy);

        return result;
    }

    [Pure]
    public RSI.State GetFallbackState()
    {
        return _resourceCache.GetFallback<RSIResource>().RSI["error"];
    }

    public Texture GetFallbackTexture()
    {
        return _resourceCache.GetFallback<TextureResource>().Texture;
    }

    [Pure]
    public RSI.State GetState(SpriteSpecifier.Rsi rsiSpecifier)
    {
        if (_resourceCache.TryGetResource<RSIResource>(
                TextureRoot / rsiSpecifier.RsiPath,
                out var theRsi) &&
            theRsi.RSI.TryGetState(rsiSpecifier.RsiState, out var state))
        {
            return state;
        }

        _sawmill.Error("Failed to load RSI {0}", rsiSpecifier.RsiPath);
        return GetFallbackState();
    }

    public Texture GetTexture(SpriteSpecifier.Texture texSpecifier)
    {
        return _resourceCache
            .GetResource<TextureResource>(TextureRoot / texSpecifier.TexturePath)
            .Texture;
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.TryGetModified<EntityPrototype>(out var modified))
            return;

        // Remove all changed prototypes from the cache, if they're there.
        foreach (var prototype in modified)
        {
            // Let's be lazy and not regenerate them until something needs them again.
            _cachedPrototypeIcons.Remove(prototype);
        }
    }

    /// <summary>
    /// Gets an entity's sprite position in world terms.
    /// </summary>
    public Vector2 GetSpriteWorldPosition(Entity<SpriteComponent?, TransformComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp2))
            return Vector2.Zero;

        var (worldPos, worldRot) = _xforms.GetWorldPositionRotation(entity.Owner);

        if (!Resolve(entity, ref entity.Comp1, false))
        {
            return worldPos;
        }

        if (entity.Comp1.NoRotation)
        {
            return worldPos + entity.Comp1.Offset;
        }

        return worldPos + worldRot.RotateVec(entity.Comp1.Rotation.RotateVec(entity.Comp1.Offset));
    }

    /// <summary>
    /// Gets an entity's sprite position in screen coordinates.
    /// </summary>
    public ScreenCoordinates GetSpriteScreenCoordinates(Entity<SpriteComponent?, TransformComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp2))
            return ScreenCoordinates.Invalid;

        var spriteCoords = GetSpriteWorldPosition(entity);
        return _eye.MapToScreen(new MapCoordinates(spriteCoords, entity.Comp2.MapID));
    }
}
