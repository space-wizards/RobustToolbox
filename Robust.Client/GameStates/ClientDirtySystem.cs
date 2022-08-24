using System.Collections.Generic;
using Robust.Client.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Robust.Client.GameStates;

/// <summary>
/// Tracks dirty entities on the client for the purposes of gamestatemanager.
/// </summary>
internal sealed class ClientDirtySystem : EntitySystem
{
    [Dependency] private readonly IClientGameTiming _timing = default!;
    [Dependency] private readonly IComponentFactory _compFact = default!;
    
    // Entities that have removed networked components
    // could pool the ushort sets, but predicted component changes are rare... soo...
    internal readonly Dictionary<EntityUid, HashSet<ushort>> RemovedComponents = new();

    internal readonly HashSet<EntityUid> DirtyEntities = new(256);

    public override void Initialize()
    {
        base.Initialize();
        EntityManager.EntityDirtied += OnEntityDirty;
        EntityManager.ComponentRemoved += OnCompRemoved;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        EntityManager.EntityDirtied -= OnEntityDirty;
        EntityManager.ComponentRemoved -= OnCompRemoved;
        DirtyEntities.Clear();
    }

    private void OnCompRemoved(RemovedComponentEventArgs args)
    {
        if (args.BaseArgs.Owner.IsClientSide() || !args.BaseArgs.Component.NetSyncEnabled || !_timing.InPrediction)
            return;

        // Was this component added during prediction? If yes, then there is no need to re-add it when resetting.
        if (args.BaseArgs.Component.CreationTick > _timing.LastRealTick)
            return;

        // TODO if ever entity deletion gets predicted... add an arg to comp removal that specifies whether removal is
        // occurring because of entity deletion, to speed this function up, as it will get called once for each
        // component the entity had.
        // aka: I don't want to have to fetch the meta-data component 10+ times for each entity that gets deleted.
        //
        // If we have predicted deletions: check here that the entity is not terminating.

        var netId = _compFact.GetRegistration(args.BaseArgs.Component).NetID;
        if (netId == null)
            return;

        RemovedComponents.GetOrNew(args.BaseArgs.Owner).Add(netId.Value);
    }

    internal void Reset()
    {
        DirtyEntities.Clear();
        RemovedComponents.Clear();
    }

    private void OnEntityDirty(EntityUid e)
    {
        if (!e.IsClientSide() && _timing.InPrediction)
            DirtyEntities.Add(e);
    }
}
