using Robust.Server.GameStates;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Server.GameObjects;

public sealed class ServerMetaDataSystem : MetaDataSystem
{
    [Dependency] private readonly PVSSystem _pvsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MetaDataComponent, PlayerAttachedEvent>(OnActorPlayerAttach);

        EntityManager.ComponentAdded += OnComponentAdded;
        EntityManager.ComponentRemoved += OnComponentRemoved;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        EntityManager.ComponentAdded -= OnComponentAdded;
        EntityManager.ComponentRemoved -= OnComponentRemoved;
    }

    private void OnComponentAdded(AddedComponentEventArgs obj)
    {
        var comp = obj.BaseArgs.Component;
        if (comp.NetSyncEnabled && (comp.SessionSpecific || comp.SendOnlyToOwner))
            MetaData(obj.BaseArgs.Owner).Flags |= MetaDataFlags.SessionSpecific;
    }

    private void OnComponentRemoved(RemovedComponentEventArgs obj)
    {
        var removed = obj.BaseArgs.Component;
        if (!removed.NetSyncEnabled || (!removed.SessionSpecific && !removed.SendOnlyToOwner))
            return;

        var meta = MetaData(obj.BaseArgs.Owner);
        if (meta.EntityLifeStage >= EntityLifeStage.Terminating)
            return;

        foreach (var (_, comp) in EntityManager.GetNetComponents(obj.BaseArgs.Owner))
        {
            if (comp.LifeStage >= ComponentLifeStage.Removing)
                continue;

            if (comp.NetSyncEnabled && (comp.SessionSpecific || comp.SendOnlyToOwner))
                return; // keep the flag
        }

        // remove the flag
        meta.Flags &= ~MetaDataFlags.SessionSpecific;
    }

    private void OnActorPlayerAttach(EntityUid uid, MetaDataComponent meta, PlayerAttachedEvent args)
    {
        if ((meta.Flags & MetaDataFlags.SessionSpecific) == 0)
            return;

        // A new player has been attached. In order to ensure that this player receives session-restricted entity
        // states, we will dirty any restricted components.

        foreach (var (_, comp) in EntityManager.GetNetComponents(uid))
        {
            if (comp.SessionSpecific || comp.SendOnlyToOwner)
                Dirty(comp);
        }
    }

    public override void SetVisibilityMask(EntityUid uid, int value, MetaDataComponent? meta = null)
    {
        if (!Resolve(uid, ref meta) || meta.VisibilityMask == value)
            return;

        base.SetVisibilityMask(uid, value, meta);
        _pvsSystem.MarkDirty(uid);
    }
}
