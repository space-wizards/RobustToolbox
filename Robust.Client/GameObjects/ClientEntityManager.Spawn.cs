using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects;

public sealed partial class ClientEntityManager
{
    public override EntityUid PredictedSpawnAttachedTo(string? protoName, EntityCoordinates coordinates, ComponentRegistry? overrides = null, Angle rotation = default)
    {
        var ent = SpawnAttachedTo(protoName, coordinates, overrides, rotation);
        FlagPredicted(ent);
        return ent;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override EntityUid PredictedSpawn(string? protoName = null, ComponentRegistry? overrides = null, bool doMapInit = true)
    {
        var ent = Spawn(protoName, overrides, doMapInit);
        FlagPredicted(ent);
        return ent;
    }

    public override EntityUid PredictedSpawn(string? protoName, MapCoordinates coordinates, ComponentRegistry? overrides = null, Angle rotation = default!)
    {
        var ent = Spawn(protoName, coordinates, overrides, rotation);
        FlagPredicted(ent);
        return ent;
    }

    public override EntityUid PredictedSpawnAtPosition(string? protoName, EntityCoordinates coordinates, ComponentRegistry? overrides = null)
    {
        var ent = SpawnAtPosition(protoName, coordinates, overrides);
        FlagPredicted(ent);
        return ent;
    }

    public override bool PredictedTrySpawnNextTo(
        string? protoName,
        EntityUid target,
        [NotNullWhen(true)] out EntityUid? uid,
        TransformComponent? xform = null,
        ComponentRegistry? overrides = null)
    {
        if (!TrySpawnNextTo(protoName, target, out uid, xform, overrides))
            return false;

        FlagPredicted(uid.Value);
        return true;
    }

    public override bool PredictedTrySpawnInContainer(
        string? protoName,
        EntityUid containerUid,
        string containerId,
        [NotNullWhen(true)] out EntityUid? uid,
        ContainerManagerComponent? containerComp = null,
        ComponentRegistry? overrides = null)
    {
        if (!TrySpawnInContainer(protoName, containerUid, containerId, out uid, containerComp, overrides))
            return false;

        FlagPredicted(uid.Value);
        return true;
    }

    public override EntityUid PredictedSpawnNextToOrDrop(string? protoName, EntityUid target, TransformComponent? xform = null, ComponentRegistry? overrides = null)
    {
        var ent = SpawnNextToOrDrop(protoName, target, xform, overrides);
        FlagPredicted(ent);
        return ent;
    }

    public override EntityUid PredictedSpawnInContainerOrDrop(
        string? protoName,
        EntityUid containerUid,
        string containerId,
        TransformComponent? xform = null,
        ContainerManagerComponent? containerComp = null,
        ComponentRegistry? overrides = null)
    {
        var ent = SpawnInContainerOrDrop(protoName, containerUid, containerId, xform, containerComp, overrides);
        FlagPredicted(ent);
        return ent;
    }

    public override EntityUid PredictedSpawnInContainerOrDrop(
        string? protoName,
        EntityUid containerUid,
        string containerId,
        out bool inserted,
        TransformComponent? xform = null,
        ContainerManagerComponent? containerComp = null,
        ComponentRegistry? overrides = null)
    {
        var ent = SpawnInContainerOrDrop(protoName,
            containerUid,
            containerId,
            out inserted,
            xform,
            containerComp,
            overrides);

        FlagPredicted(ent);
        return ent;
    }

    public override void FlagPredicted(Entity<MetaDataComponent?> ent)
    {
        if (!MetaQuery.Resolve(ent.Owner, ref ent.Comp))
            return;

        DebugTools.Assert(IsClientSide(ent.Owner, ent.Comp));
        EnsureComponent<PredictedSpawnComponent>(ent.Owner);

        // TODO: Need to map call site or something, needs to be consistent between client and server.
    }
}
