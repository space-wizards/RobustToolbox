using System.Collections.Generic;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Dynamics;
using Color = Robust.Shared.Maths.Color;

namespace Robust.Client.GameObjects;

public sealed class DebugEntityLookupCommand : LocalizedEntityCommands
{
    [Dependency] private readonly DebugEntityLookupSystem _system = default!;

    public override string Command => "togglelookup";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _system.Enabled ^= true;
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
                        EntityManager.System<EntityLookupSystem>(),
                        EntityManager.System<SharedTransformSystem>()));
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
    private readonly IEntityManager _entityManager;
    private readonly EntityLookupSystem _lookup;
    private readonly SharedTransformSystem _transform;

    private EntityQuery<TransformComponent> _xformQuery;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public EntityLookupOverlay(IEntityManager entManager, EntityLookupSystem lookup, SharedTransformSystem transform)
    {
        _entityManager = entManager;
        _lookup = lookup;
        _xformQuery = entManager.GetEntityQuery<TransformComponent>();
        _transform = transform;
    }

    protected internal override void Draw(in OverlayDrawArgs args)
    {
        var worldHandle = args.WorldHandle;
        var worldBounds = args.WorldBounds;

        // TODO: Static version
        _lookup.FindLookupsIntersecting(args.MapId, worldBounds, (uid, lookup) =>
        {
            var (_, rotation, matrix, invMatrix) = _transform.GetWorldPositionRotationMatrixWithInv(uid);

            worldHandle.SetTransform(matrix);

            var lookupAABB = invMatrix.TransformBox(worldBounds);
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
                if (_entityManager.Deleted(ent))
                    continue;

                var xform = _xformQuery.GetComponent(ent);

                //DebugTools.Assert(!ent.IsInContainer(_entityManager));
                var (entPos, entRot) = _transform.GetWorldPositionRotation(ent);

                var lookupPos = Vector2.Transform(entPos, invMatrix);
                var lookupRot = entRot - rotation;

                var aabb = _lookup.GetAABB(ent, lookupPos, lookupRot, xform, _xformQuery);

                worldHandle.DrawRect(aabb, Color.Blue.WithAlpha(0.2f));
            }
        });

        worldHandle.SetTransform(Matrix3x2.Identity);
    }
}
