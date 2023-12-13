// MIT License

// Copyright (c) 2019 Erin Catto

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.


using System;
using System.Numerics;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Robust.Server.Console.Commands
{
    /*
     * I didn't use blueprints because this is way easier to iterate upon as I can shit out testbed upon testbed on new maps
     * and never have to leave my debugger.
     */

    /// <summary>
    ///     Copies of Box2D's physics testbed for debugging.
    /// </summary>
    public sealed class TestbedCommand : LocalizedCommands
    {
        [Dependency] private readonly IEntityManager _ent = default!;
        [Dependency] private readonly IMapManager _map = default!;

        public override string Command => "testbed";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteError("Require 2 args for testbed!");
                return;
            }

            if (!int.TryParse(args[0], out var mapInt))
            {
                shell.WriteError($"Unable to parse map {args[0]}");
                return;
            }

            var mapId = new MapId(mapInt);

            if (shell.Player == null)
            {
                shell.WriteError("No player found");
                return;
            }

            Action testbed;
            SetupPlayer(mapId, shell);

            switch (args[1])
            {
                case "boxstack":
                    testbed = () => CreateBoxStack(mapId);
                    break;
                case "circlestack":
                    testbed = () => CreateCircleStack(mapId);
                    break;
                case "pyramid":
                    testbed = () => CreatePyramid(mapId);
                    break;
                case "tumbler":
                    testbed = () => CreateTumbler(mapId);
                    break;
                default:
                    shell.WriteError($"testbed {args[0]} not found!");
                    return;
            }

            Timer.Spawn(1000, () =>
            {
                if (!_map.MapExists(mapId)) return;
                testbed();
            });

            shell.WriteLine($"Testbed on map {mapId}");
        }

        private void SetupPlayer(MapId mapId, IConsoleShell shell)
        {
            if (mapId == MapId.Nullspace) return;

            if (!_map.MapExists(mapId))
            {
                _map.CreateMap(mapId);
            }

            _map.SetMapPaused(mapId, false);
            var mapUid = _map.GetMapEntityIdOrThrow(mapId);
            _ent.System<Gravity2DController>().SetGravity(mapUid, new Vector2(0, -9.8f));

            shell.ExecuteCommand("aghost");
            shell.ExecuteCommand($"tp 0 0 {mapId}");
            shell.RemoteExecuteCommand($"physics shapes");

            return;
        }

        private void CreateBoxStack(MapId mapId)
        {
            var physics = _ent.System<SharedPhysicsSystem>();
            var fixtures = _ent.System<FixtureSystem>();

            var groundUid = _ent.SpawnEntity(null, new MapCoordinates(0, 0, mapId));
            var ground = _ent.AddComponent<PhysicsComponent>(groundUid);

            var horizontal = new EdgeShape(new Vector2(-40, 0), new Vector2(40, 0));
            fixtures.CreateFixture(groundUid, "fix1", new Fixture(horizontal, 2, 2, true), body: ground);

            var vertical = new EdgeShape(new Vector2(10, 0), new Vector2(10, 10));
            fixtures.CreateFixture(groundUid, "fix2", new Fixture(vertical, 2, 2, true), body: ground);

            var xs = new[]
            {
                0.0f, -10.0f, -5.0f, 5.0f, 10.0f
            };

            var columnCount = 1;
            var rowCount = 15;
            PolygonShape shape;

            for (var j = 0; j < columnCount; j++)
            {
                for (var i = 0; i < rowCount; i++)
                {
                    var x = 0.0f;

                    var boxUid = _ent.SpawnEntity(null,
                        new MapCoordinates(new Vector2(xs[j] + x, 0.55f + 1.1f * i), mapId));
                    var box = _ent.AddComponent<PhysicsComponent>(boxUid);

                    physics.SetBodyType(boxUid, BodyType.Dynamic, body: box);

                    shape = new PolygonShape();
                    shape.SetAsBox(0.5f, 0.5f);
                    physics.SetFixedRotation(boxUid, false, body: box);
                    fixtures.CreateFixture(boxUid, "fix1", new Fixture(shape, 2, 2, true), body: box);

                    physics.WakeBody(boxUid, body: box);
                }
            }

            physics.WakeBody(groundUid, body: ground);
        }

        private void CreateCircleStack(MapId mapId)
        {
            var physics = _ent.System<SharedPhysicsSystem>();
            var fixtures = _ent.System<FixtureSystem>();

            var groundUid = _ent.SpawnEntity(null, new MapCoordinates(0, 0, mapId));
            var ground = _ent.AddComponent<PhysicsComponent>(groundUid);

            var horizontal = new EdgeShape(new Vector2(-40, 0), new Vector2(40, 0));
            fixtures.CreateFixture(groundUid, "fix1", new Fixture(horizontal, 2, 2, true), body: ground);

            var vertical = new EdgeShape(new Vector2(20, 0), new Vector2(20, 20));
            fixtures.CreateFixture(groundUid, "fix2", new Fixture(vertical, 2, 2, true), body: ground);

            var xs = new[]
            {
                0.0f, -10.0f, -5.0f, 5.0f, 10.0f
            };

            var columnCount = 1;
            var rowCount = 15;
            PhysShapeCircle shape;

            for (var j = 0; j < columnCount; j++)
            {
                for (var i = 0; i < rowCount; i++)
                {
                    var x = 0.0f;

                    var boxUid = _ent.SpawnEntity(null,
                        new MapCoordinates(new Vector2(xs[j] + x, 0.55f + 2.1f * i), mapId));
                    var box = _ent.AddComponent<PhysicsComponent>(boxUid);

                    physics.SetBodyType(boxUid, BodyType.Dynamic, body: box);
                    shape = new PhysShapeCircle(0.5f);
                    physics.SetFixedRotation(boxUid, false, body: box);
                    // TODO: Need to detect shape and work out if we need to use fixedrotation

                    fixtures.CreateFixture(boxUid, "fix1", new Fixture(shape, 2, 2, true, 5f));
                    physics.WakeBody(boxUid, body: box);
                }
            }

            physics.WakeBody(groundUid, body: ground);
        }

        private void CreatePyramid(MapId mapId)
        {
            const byte count = 20;

            // Setup ground
            var physics = _ent.System<SharedPhysicsSystem>();
            var fixtures = _ent.System<FixtureSystem>();
            var groundUid = _ent.SpawnEntity(null, new MapCoordinates(0, 0, mapId));
            var ground = _ent.AddComponent<PhysicsComponent>(groundUid);

            var horizontal = new EdgeShape(new Vector2(40, 0), new Vector2(-40, 0));
            fixtures.CreateFixture(groundUid, "fix1", new Fixture(horizontal, 2, 2, true), body: ground);
            physics.WakeBody(groundUid, body: ground);

            // Setup boxes
            float a = 0.5f;
            PolygonShape shape = new();
            shape.SetAsBox(a, a);

            var x = new Vector2(-7.0f, 0.75f);
            Vector2 y;
            Vector2 deltaX = new Vector2(0.5625f, 1.25f);
            Vector2 deltaY = new Vector2(1.125f, 0.0f);

            for (var i = 0; i < count; ++i)
            {
                y = x;

                for (var j = i; j < count; ++j)
                {
                    var boxUid = _ent.SpawnEntity(null, new MapCoordinates(y, mapId));
                    var box = _ent.AddComponent<PhysicsComponent>(boxUid);
                    physics.SetBodyType(boxUid, BodyType.Dynamic, body: box);

                    fixtures.CreateFixture(boxUid, "fix1", new Fixture(shape, 2, 2, true, 5f), body: box);
                    y += deltaY;

                    physics.WakeBody(boxUid, body: box);
                }

                x += deltaX;
            }
        }

        private void CreateTumbler(MapId mapId)
        {
            var physics = _ent.System<SharedPhysicsSystem>();
            var fixtures = _ent.System<FixtureSystem>();
            var joints = _ent.System<SharedJointSystem>();

            var groundUid = _ent.SpawnEntity(null, new MapCoordinates(0f, 0f, mapId));
            var ground = _ent.AddComponent<PhysicsComponent>(groundUid);
            // Due to lookup changes fixtureless bodies are invalid, so
            var cShape = new PhysShapeCircle(1f);
            fixtures.CreateFixture(groundUid, "fix1", new Fixture(cShape, 0, 0, false));

            var bodyUid = _ent.SpawnEntity(null, new MapCoordinates(0f, 10f, mapId));
            var body = _ent.AddComponent<PhysicsComponent>(bodyUid);

            physics.SetBodyType(bodyUid, BodyType.Dynamic, body: body);
            physics.SetSleepingAllowed(bodyUid, body, false);
            physics.SetFixedRotation(bodyUid, false, body: body);


            // TODO: Box2D just deref, bleh shape structs someday
            var shape1 = new PolygonShape();
            shape1.SetAsBox(0.5f, 10.0f, new Vector2(10.0f, 0.0f), 0.0f);
            fixtures.CreateFixture(bodyUid, "fix1", new Fixture(shape1, 2, 0, true, 20f));

            var shape2 = new PolygonShape();
            shape2.SetAsBox(0.5f, 10.0f, new Vector2(-10.0f, 0.0f), 0f);
            fixtures.CreateFixture(bodyUid, "fix2", new Fixture(shape2, 2, 0, true, 20f));

            var shape3 = new PolygonShape();
            shape3.SetAsBox(10.0f, 0.5f, new Vector2(0.0f, 10.0f), 0f);
            fixtures.CreateFixture(bodyUid, "fix3", new Fixture(shape3, 2, 0, true, 20f));

            var shape4 = new PolygonShape();
            shape4.SetAsBox(10.0f, 0.5f, new Vector2(0.0f, -10.0f), 0f);
            fixtures.CreateFixture(bodyUid, "fix4", new Fixture(shape4, 2, 0, true, 20f));

            physics.WakeBody(groundUid, body: ground);
            physics.WakeBody(bodyUid, body: body);
            var revolute = joints.CreateRevoluteJoint(groundUid, bodyUid);
            revolute.LocalAnchorA = new Vector2(0f, 10f);
            revolute.LocalAnchorB = new Vector2(0f, 0f);
            revolute.ReferenceAngle = 0f;
            revolute.MotorSpeed = 0.05f * MathF.PI;
            revolute.MaxMotorTorque = 100000000f;
            revolute.EnableMotor = true;

            // Box2D has this as 800 which is jesus christo.
            // Wouldn't recommend higher than 100 in debug and higher than 300 on release unless
            // you really want a profile.
            var count = 300;

            for (var i = 0; i < count; i++)
            {
                Timer.Spawn(i * 20, () =>
                {
                    if (!_map.MapExists(mapId)) return;
                    var boxUid = _ent.SpawnEntity(null, new MapCoordinates(0f, 10f, mapId));
                    var box = _ent.AddComponent<PhysicsComponent>(boxUid);
                    physics.SetBodyType(boxUid, BodyType.Dynamic, body: box);
                    physics.SetFixedRotation(boxUid, false, body: box);
                    var shape = new PolygonShape();
                    shape.SetAsBox(0.125f, 0.125f);
                    fixtures.CreateFixture(boxUid, "fix1", new Fixture(shape, 2, 2, true, 0.0625f), body: box);
                    physics.WakeBody(boxUid, body: box);
                });
            }
        }
    }
}
