using Robust.Client.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;

namespace Robust.Client.GameStates;

/// <summary>
/// Tracks dirty entities on the client for the purposes of gamestatemanager.
/// </summary>
public sealed class ClientDirtySystem : EntitySystem
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
        SubscribeLocalEvent<EntityTerminatingEvent>(OnTerminate);
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

    private void OnTerminate(ref EntityTerminatingEvent ev)
    {
        if (!_timing.InPrediction || IsClientSide(ev.Entity))
            return;

        // Client-side entity deletion is not supported and will cause errors.
        Log.Error($"Predicting the deletion of a networked entity: {ToPrettyString(ev.Entity.Owner, ev.Entity.Comp)}. Trace: {Environment.StackTrace}");
    }

    private void OnCompRemoved(RemovedComponentEventArgs args)
    {
        if (args.Terminating)
            return;

        var uid = args.BaseArgs.Owner;
        var comp = args.BaseArgs.Component;
        if (!_timing.InPrediction || !comp.NetSyncEnabled || IsClientSide(uid, args.Meta))
            return;

        // Was this component added during prediction? If yes, then there is no need to re-add it when resetting.
        if (comp.CreationTick > _timing.LastRealTick)
            return;

        var netId = _compFact.GetRegistration(comp).NetID;
        if (netId != null)
            RemovedComponents.GetOrNew(uid).Add(netId.Value);
    }

    public void Reset()
    {
        DirtyEntities.Clear();
        RemovedComponents.Clear();
    }

    private void OnEntityDirty(Entity<MetaDataComponent> e)
    {
        if (_timing.InPrediction && !IsClientSide(e, e))
            DirtyEntities.Add(e);
    }
}
