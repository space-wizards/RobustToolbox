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
        Reset();
    }

    private void OnCompRemoved(RemovedComponentEventArgs args)
    {
        // TODO if entity deletion ever gets predicted, enable this check. Currently it is redundant:
        // if (args.Terminating)
        //    return;

        var comp = args.BaseArgs.Component;
        if (!_timing.InPrediction || comp.Owner.IsClientSide() || !comp.NetSyncEnabled)
            return;

        // Was this component added during prediction? If yes, then there is no need to re-add it when resetting.
        if (comp.CreationTick > _timing.LastRealTick)
            return;

        var netId = _compFact.GetRegistration(comp).NetID;
        if (netId != null)
            RemovedComponents.GetOrNew(comp.Owner).Add(netId.Value);
    }

    internal void Reset()
    {
        DirtyEntities.Clear();
        RemovedComponents.Clear();
    }

    private void OnEntityDirty(EntityUid e)
    {
        if (_timing.InPrediction && !e.IsClientSide())
            DirtyEntities.Add(e);
    }
}
