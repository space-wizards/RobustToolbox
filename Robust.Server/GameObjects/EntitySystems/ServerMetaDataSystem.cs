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

    /// <summary>
    ///     If a session-specific component gets added, make sure the meta-data flag is set.
    /// </summary>
    private void OnComponentAdded(AddedComponentEventArgs obj)
    {
        var comp = obj.BaseArgs.Component;
        if (comp.NetSyncEnabled && (comp.SessionSpecific || comp.SendOnlyToOwner))
            MetaData(obj.BaseArgs.Owner).Flags |= MetaDataFlags.SessionSpecific;
    }

    /// <summary>
    ///     If a session-specific component gets removed, this will update the meta-data flag.
    /// </summary>
    private void OnComponentRemoved(RemovedComponentEventArgs obj)
    {
        var removed = obj.BaseArgs.Component;
        if (obj.Terminating || !removed.NetSyncEnabled || (!removed.SessionSpecific && !removed.SendOnlyToOwner))
            return;

        foreach (var (_, comp) in EntityManager.GetNetComponents(obj.BaseArgs.Owner))
        {
            if (comp.LifeStage >= ComponentLifeStage.Removing)
                continue;

            if (comp.NetSyncEnabled && (comp.SessionSpecific || comp.SendOnlyToOwner))
                return; // keep the flag
        }

        // remove the flag
        MetaData(obj.BaseArgs.Owner).Flags &= ~MetaDataFlags.SessionSpecific;
    }

    /// <summary>
    ///     If a new player gets attached to an entity, this will ensure that the player receives session-restricted
    ///     component states by dirtying any restricted components. 
    /// </summary>
    private void OnActorPlayerAttach(EntityUid uid, MetaDataComponent meta, PlayerAttachedEvent args)
    {
        if ((meta.Flags & MetaDataFlags.SessionSpecific) == 0)
            return;

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
