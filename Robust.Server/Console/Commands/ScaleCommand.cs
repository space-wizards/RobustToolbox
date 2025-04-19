using System;
using System.Numerics;
using Robust.Server.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Systems;

namespace Robust.Server.Console.Commands;

public sealed class ScaleCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override string Command => "scale";

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        switch (args.Length)
        {
            case 1:
                return CompletionResult.FromOptions(CompletionHelper.NetEntities(args[0], entManager: _entityManager));
            case 2:
                return CompletionResult.FromHint(Loc.GetString("cmd-hint-float"));
            default:
                return CompletionResult.Empty;
        }
    }

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError($"Insufficient number of args supplied: expected 2 and received {args.Length}");
            return;
        }

        if (!NetEntity.TryParse(args[0], out var netEntity))
        {
            shell.WriteError($"Unable to find entity {args[0]}");
            return;
        }

        if (!float.TryParse(args[1], out var scale))
        {
            shell.WriteError($"Invalid scale supplied of {args[0]}");
            return;
        }

        if (scale < 0f)
        {
            shell.WriteError($"Invalid scale supplied that is negative!");
            return;
        }

        // Event for content to use
        // We'll just set engine stuff here
        var physics = _entityManager.System<SharedPhysicsSystem>();
        var appearance = _entityManager.System<AppearanceSystem>();

        var uid = _entityManager.GetEntity(netEntity);
        _entityManager.EnsureComponent<ScaleVisualsComponent>(uid);
        var @event = new ScaleEntityEvent();
        _entityManager.EventBus.RaiseLocalEvent(uid, ref @event);

        var appearanceComponent = _entityManager.EnsureComponent<AppearanceComponent>(uid);
        if (!appearance.TryGetData<Vector2>(uid, ScaleVisuals.Scale, out var oldScale, appearanceComponent))
            oldScale = Vector2.One;

        appearance.SetData(uid, ScaleVisuals.Scale, oldScale * scale, appearanceComponent);

        if (_entityManager.TryGetComponent(uid, out FixturesComponent? manager))
        {
            foreach (var (id, fixture) in manager.Fixtures)
            {
                switch (fixture.Shape)
                {
                    case EdgeShape edge:
                        physics.SetVertices(uid, id, fixture,
                            edge,
                            edge.Vertex0 * scale,
                            edge.Vertex1 * scale,
                            edge.Vertex2 * scale,
                            edge.Vertex3 * scale, manager);
                        break;
                    case PhysShapeCircle circle:
                        physics.SetPositionRadius(uid, id, fixture, circle, circle.Position * scale, circle.Radius * scale, manager);
                        break;
                    case PolygonShape poly:
                        var verts = poly.Vertices;

                        for (var i = 0; i < poly.VertexCount; i++)
                        {
                            verts[i] *= scale;
                        }

                        physics.SetVertices(uid, id, fixture, poly, verts, manager);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
    }

    [ByRefEvent]
    public readonly record struct ScaleEntityEvent(EntityUid Uid) {}
}
