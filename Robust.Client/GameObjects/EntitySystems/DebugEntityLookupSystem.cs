using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Shared.Console;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Utility;
using Color = Robust.Shared.Maths.Color;

namespace Robust.Client.GameObjects;

public sealed class DebugEntityLookupCommand : LocalizedCommands
{
    public override string Command => "togglelookup";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        EntitySystem.Get<DebugEntityLookupSystem>().Enabled ^= true;
    }
}

public sealed class DebugEntityLookupSystem : EntitySystem
{
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value) return;

            _enabled = value;

            if (_enabled)
            {
                IoCManager.Resolve<IOverlayManager>().AddOverlay(
                    new EntityLookupOverlay(
                        EntityManager,
                        Get<EntityLookupSystem>()));
            }
            else
            {
                IoCManager.Resolve<IOverlayManager>().RemoveOverlay<EntityLookupOverlay>();
            }
        }
    }

    private bool _enabled;
}

public sealed class EntityLookupOverlay : Overlay
{
    private IEntityManager _entityManager = default!;
    private EntityLookupSystem _lookup = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public EntityLookupOverlay(IEntityManager entManager, EntityLookupSystem lookup)
    {
        _entityManager = entManager;
        _lookup = lookup;
    }

    protected internal override void Draw(in OverlayDrawArgs args)
    {
        var worldHandle = args.WorldHandle;
        var xformQuery = _entityManager.GetEntityQuery<TransformComponent>();

        foreach (var lookup in _lookup.FindLookupsIntersecting(args.MapId, args.WorldBounds))
        {
            var lookupXform = xformQuery.GetComponent(lookup.Owner);

            var (_, rotation, matrix, invMatrix) = lookupXform.GetWorldPositionRotationMatrixWithInv();

            worldHandle.SetTransform(matrix);

            var lookupAABB = invMatrix.TransformBox(args.WorldBounds);
            var ents = new List<EntityUid>();

            lookup.DynamicTree.QueryAabb(ref ents, static (ref List<EntityUid> state, in FixtureProxy value) =>
            {
                state.Add(value.Entity);
                return true;
            }, lookupAABB);

            lookup.StaticTree.QueryAabb(ref ents, static (ref List<EntityUid> state, in FixtureProxy value) =>
            {
                state.Add(value.Entity);
                return true;
            }, lookupAABB);

            lookup.StaticSundriesTree.QueryAabb(ref ents, static (ref List<EntityUid> state, in EntityUid value) =>
            {
                state.Add(value);
                return true;
            }, lookupAABB);

            lookup.SundriesTree.QueryAabb(ref ents, static (ref List<EntityUid> state, in EntityUid value) =>
            {
                state.Add(value);
                return true;
            }, lookupAABB);

            foreach (var ent in ents)
            {
                if (_entityManager.Deleted(ent)) continue;
                var xform = xformQuery.GetComponent(ent);

                //DebugTools.Assert(!ent.IsInContainer(_entityManager));
                var (entPos, entRot) = xform.GetWorldPositionRotation();

                var lookupPos = invMatrix.Transform(entPos);
                var lookupRot = entRot - rotation;

                var aabb = _lookup.GetAABB(ent, lookupPos, lookupRot, xform, xformQuery);

                worldHandle.DrawRect(aabb, Color.Blue.WithAlpha(0.2f));
            }
        }

        worldHandle.SetTransform(Matrix3.Identity);
    }
}
