using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
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
        SubscribeLocalEvent<ActorComponent, ComponentGetState>(OnActorGetState);
        SubscribeLocalEvent<ActorComponent, ComponentHandleState>(OnActorHandleState);
    }

    private void OnActorGetState(Entity<ActorComponent> ent, ref ComponentGetState args)
    {
        var interfaces = new Dictionary<NetEntity, List<Enum>>();

        foreach (var (buid, data) in ent.Comp.OpenInterfaces)
        {
            interfaces[GetNetEntity(buid)] = data;
        }

        args.State = new ActorComponent.ActorComponentState()
        {
            OpenInterfaces = interfaces,
        };
    }

    private void OnActorHandleState(Entity<ActorComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not ActorComponent.ActorComponentState state)
            return;

        // TODO: Allocate less.
        ent.Comp.OpenInterfaces.Clear();

        foreach (var (nent, data) in state.OpenInterfaces)
        {
            var openEnt = EnsureEntity<ActorComponent>(nent, ent.Owner);
            ent.Comp.OpenInterfaces[openEnt] = data;
        }
    }

    private void OnActorShutdown(EntityUid entity, ActorComponent component, ComponentShutdown args)
    {
        _playerManager.SetAttachedEntity(component.PlayerSession, null);
    }
}
