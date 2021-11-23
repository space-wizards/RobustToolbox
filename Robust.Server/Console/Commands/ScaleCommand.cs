using System;
using Robust.Server.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics.Collision.Shapes;

namespace Robust.Server.Console.Commands;

public sealed class ScaleCommand : IConsoleCommand
{
    public string Command => "scale";
    public string Description => "Increases or decreases an entity's size naively";
    public string Help => $"{Command} <entityUid> <float>";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError($"Insufficient number of args supplied: expected 2 and received {args.Length}");
            return;
        }

        if (!EntityUid.TryParse(args[0], out var uid))
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
        var @event = new ScaleEntityEvent();
        var entManager = IoCManager.Resolve<IEntityManager>();
        entManager.EventBus.RaiseLocalEvent(uid, ref @event);

        if (entManager.TryGetComponent(uid, out SpriteComponent? spriteComponent))
        {
            spriteComponent.Scale *= scale;
        }

        if (entManager.TryGetComponent(uid, out PhysicsComponent? body))
        {
            foreach (var fixture in body._fixtures)
            {
                switch (fixture.Shape)
                {
                    case EdgeShape edge:
                        edge.Vertex0 *= scale;
                        edge.Vertex1 *= scale;
                        edge.Vertex2 *= scale;
                        edge.Vertex3 *= scale;
                        break;
                    case PhysShapeCircle circle:
                        circle.Position *= scale;
                        circle.Radius *= scale;
                        break;
                    case PolygonShape poly:
                        var verts = poly.Vertices;

                        for (var i = 0; i < verts.Length; i++)
                        {
                            verts[i] *= scale;
                        }

                        poly.SetVertices(verts);
                        break;
                    default:
                        throw new NotImplementedException();
                }

                body.FixtureChanged(fixture);
            }
        }
    }

    public readonly struct ScaleEntityEvent
    {
        public readonly EntityUid Uid;

        public ScaleEntityEvent(EntityUid uid)
        {
            Uid = uid;
        }
    }
}
