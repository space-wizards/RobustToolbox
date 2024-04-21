using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Shared.Player;

/// <summary>
///     System that handles <see cref="ActorComponent"/>.
/// </summary>
public sealed class ActorSystem : EntitySystem
{
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ActorComponent, ComponentShutdown>(OnActorShutdown);
    }

    private void OnActorShutdown(EntityUid entity, ActorComponent component, ComponentShutdown args)
    {
        _playerManager.SetAttachedEntity(component.PlayerSession, null);
    }
}
