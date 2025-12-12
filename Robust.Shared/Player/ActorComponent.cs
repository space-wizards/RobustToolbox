using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Player;

/// <summary>
///     This component is added to entities that are currently controlled by a player and is removed when the player is detached.
/// </summary>
/// <seealso cref="M:Robust.Shared.Player.ISharedPlayerManager.SetAttachedEntity(Robust.Shared.Player.ICommonSession,System.Nullable{Robust.Shared.GameObjects.EntityUid},Robust.Shared.Player.ICommonSession@,System.Boolean)"/>
[RegisterComponent, UnsavedComponent]
public sealed partial class ActorComponent : Component
{
    /// <summary>
    ///     The player session currently attached to the entity.
    /// </summary>
    [ViewVariables]
    public ICommonSession PlayerSession { get; internal set; } = default!;
}
