using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;

namespace Robust.Server.Physics.Commands;

public sealed class BoxStackCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public string Command => "boxstack";
    public string Description => string.Empty;
    public string Help => "boxstack [mapid] [columns] [rows]";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 3 ||
            !int.TryParse(args[0], out var mapInt) ||
            !int.TryParse(args[1], out var columns) ||
            !int.TryParse(args[2], out var rows))
        {
            return;
        }

        var mapId = new MapId(mapInt);

        var fixtureSystem = _entManager.System<FixtureSystem>();
        var physSystem = _entManager.System<SharedPhysicsSystem>();
        physSystem.SetGravity(new Vector2(0f, -9.8f));

        var groundUid = _entManager.SpawnEntity(null, new MapCoordinates(0, 0, mapId));
        var ground = _entManager.AddComponent<PhysicsComponent>(groundUid);
        var groundManager = _entManager.EnsureComponent<FixturesComponent>(groundUid);

        var horizontal = new EdgeShape(new Vector2(-40, 0), new Vector2(40, 0));
        fixtureSystem.CreateFixture(groundUid, "fix1", new Fixture(horizontal, 1, 1, true), manager: groundManager, body: ground);

        var vertical = new EdgeShape(new Vector2(10, 0), new Vector2(10, 10));
        fixtureSystem.CreateFixture(groundUid, "fix2", new Fixture(vertical, 1, 1, true), manager: groundManager, body: ground);

        physSystem.WakeBody(groundUid, manager: groundManager, body: ground);

        var xs = new[]
        {
            0.0f, -10.0f, -5.0f, 5.0f, 10.0f
        };

        for (var j = 0; j < columns; j++)
        {
            for (var i = 0; i < rows; i++)
            {
                var x = 0.0f;

                var boxUid = _entManager.SpawnEntity(null,
                    new MapCoordinates(new Vector2(xs[j] + x, 0.55f + 2.1f * i), mapId));
                var box = _entManager.AddComponent<PhysicsComponent>(boxUid);
                var manager = _entManager.EnsureComponent<FixturesComponent>(boxUid);

                physSystem.SetBodyType(boxUid, BodyType.Dynamic, manager: manager, body: box);
                var poly = new PolygonShape(0.001f);
                poly.Set(new List<Vector2>()
                {
                    new(0.5f, -0.5f),
                    new(0.5f, 0.5f),
                    new(-0.5f, 0.5f),
                    new(-0.5f, -0.5f),
                });

                fixtureSystem.CreateFixture(boxUid, "fix1", new Fixture(poly, 1, 1, true), manager: manager, body: box);
                physSystem.WakeBody(boxUid, manager: manager, body: box);
            }
        }
    }
}
