using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Robust.Server.GameObjects;

public sealed class ServerMetaDataSystem : MetaDataSystem
{
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
        var sessionSpecific = obj.ComponentType.Restriction is StateRestriction.OwnerOnly or StateRestriction.SessionSpecific;
        if (sessionSpecific && comp.NetSyncEnabled)
            MetaData(obj.BaseArgs.Owner).Flags |= MetaDataFlags.SessionSpecific;
    }

    /// <summary>
    ///     If a session-specific component gets removed, this will update the meta-data flag.
    /// </summary>
    private void OnComponentRemoved(RemovedComponentEventArgs obj)
    {
        var removed = obj.BaseArgs.Component;
        var sessionSpecific = obj.Registration.Restriction is StateRestriction.OwnerOnly or StateRestriction.SessionSpecific;
        if (obj.Terminating || !removed.NetSyncEnabled || !sessionSpecific)
            return;

        foreach (var (comp, restriction) in obj.Meta.NetComponents.Values)
        {
            if (comp.LifeStage >= ComponentLifeStage.Removing)
                continue;

            if (comp.NetSyncEnabled && (restriction is StateRestriction.SessionSpecific or StateRestriction.OwnerOnly))
                return; // keep the flag
        }

        // remove the flag
        obj.Meta.Flags &= ~MetaDataFlags.SessionSpecific;
    }

    /// <summary>
    ///     If a new player gets attached to an entity, this will ensure that the player receives session-restricted
    ///     component states by dirtying any restricted components.
    /// </summary>
    private void OnActorPlayerAttach(EntityUid uid, MetaDataComponent meta, PlayerAttachedEvent args)
    {
        if ((meta.Flags & MetaDataFlags.SessionSpecific) == 0)
            return;

        foreach (var (_, (comp, restriction)) in meta.NetComponents)
        {
            if (restriction is StateRestriction.OwnerOnly or StateRestriction.SessionSpecific)
                Dirty(uid, comp);
        }
    }
}
