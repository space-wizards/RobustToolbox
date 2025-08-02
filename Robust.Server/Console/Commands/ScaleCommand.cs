using System;
using Robust.Server.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Systems;

namespace Robust.Server.Console.Commands;

public sealed class ScaleCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly ScaleVisualsSystem _scaleVisuals = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override string Command => "scale";

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromOptions(CompletionHelper.NetEntities(args[0], entManager: _entityManager)),
            2 => CompletionResult.FromHint(Loc.GetString("cmd-hint-float")),
            _ => CompletionResult.Empty,
        };
    }

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("cmd-invalid-arg-number-error"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var netEntity))
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-entity-exist", ("arg", args[0])));
            return;
        }

        if (!float.TryParse(args[1], out var scale) || scale < 0f)
        {
            shell.WriteError(Loc.GetString("cmd-parse-failure-float", ("arg", args[1])));
            return;
        }

        var uid = _entityManager.GetEntity(netEntity);

        var oldScale = _scaleVisuals.GetSpriteScale(uid);
        var newScale = oldScale * scale;
        _scaleVisuals.SetSpriteScale(uid, newScale);

        // adjust the fixtures
        if (_entityManager.TryGetComponent(uid, out FixturesComponent? manager))
        {
            foreach (var (id, fixture) in manager.Fixtures)
            {
                switch (fixture.Shape)
                {
                    case EdgeShape edge:
                        _physics.SetVertices(uid, id, fixture,
                            edge,
                            edge.Vertex0 * scale,
                            edge.Vertex1 * scale,
                            edge.Vertex2 * scale,
                            edge.Vertex3 * scale, manager);
                        break;
                    case PhysShapeCircle circle:
                        _physics.SetPositionRadius(uid, id, fixture, circle, circle.Position * scale, circle.Radius * scale, manager);
                        break;
                    case PolygonShape poly:
                        var verts = poly.Vertices;

                        for (var i = 0; i < poly.VertexCount; i++)
                        {
                            verts[i] *= scale;
                        }

                        _physics.SetVertices(uid, id, fixture, poly, verts, manager);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
    }
}
