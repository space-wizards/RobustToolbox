using Robust.Shared.GameObjects;

namespace Robust.Client.GameObjects;

public sealed class ActorSystem : SharedActorSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnPlayerAttached(ref PlayerAttachedEvent ev)
    {
        if (TryComp<ActorComponent>(ev.Entity, out var actor))
        {
            actor.Session = ev.Player;
        }
    }

    private void OnPlayerDetached(ref PlayerDetachedEvent ev)
    {
        if (TryComp<ActorComponent>(ev.Entity, out var actor))
        {
            actor.Session = null!;
        }
    }
}
