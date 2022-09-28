using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Robust.Client.GameObjects;

/// <summary>
///     Basic system for visually communicating client-side errors to players, so that they hopefully get reported
///     instead of just being unread error logs somewhere in the console.
/// </summary>
public sealed class ClientErrorSystem : EntitySystem
{
    /// <summary>
    ///     Raises an error event and optionally modifies the entity's sprite and description to indicate to players
    ///     that something has gone wrong.
    /// </summary>
    public void EntityError(EntityUid uid, Exception e, bool updateEntity)
    {
        RaiseLocalEvent(uid, new EntityErrorEvent(uid, e), true);

        if (!updateEntity || !TryComp(uid, out MetaDataComponent? meta))
            return;

        meta.NetSyncEnabled = false;
        meta.EntityName = $"ERROR - {ToPrettyString(uid)}";
        meta.EntityDescription = Loc.GetString("error-system-entity-description");

        if (!TryComp(uid, out SpriteComponent? sprite))
            return;

        sprite.NetSyncEnabled = false;
        var index = sprite.AddLayer("error", "/Textures/error.rsi");
        sprite.LayerSetShader(index, "unshaded");
    }
}

public sealed record EntityErrorEvent(EntityUid Entity, Exception Exception);
