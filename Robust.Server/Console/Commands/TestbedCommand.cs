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
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
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
            _ent.GetComponent<SharedPhysicsMapComponent>(mapUid).Gravity = new Vector2(0, -9.8f);

            shell.ExecuteCommand("aghost");
            shell.ExecuteCommand($"tp 0 0 {mapId}");
            shell.RemoteExecuteCommand($"physics shapes");

            return;
        }

        private void CreateBoxStack(MapId mapId)
        {
            var physics = _ent.System<SharedPhysicsSystem>();

            var groundUid = _ent.SpawnEntity(null, new MapCoordinates(0, 0, mapId));
            var ground = _ent.AddComponent<PhysicsComponent>(groundUid);

            var horizontal = new EdgeShape(new Vector2(-40, 0), new Vector2(40, 0));
            var horizontalFixture = new Fixture(ground, horizontal)
            {
                CollisionLayer = 2,
                CollisionMask = 2,
                Hard = true
            };

            var broadphase = EntitySystem.Get<FixtureSystem>();

            broadphase.CreateFixture(ground, horizontalFixture);

            var vertical = new EdgeShape(new Vector2(20, 0), new Vector2(20, 20));
            var verticalFixture = new Fixture(ground, vertical)
            {
                CollisionLayer = 2,
                CollisionMask = 2,
                Hard = true
            };

            broadphase.CreateFixture(ground, verticalFixture);

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

                    box.BodyType = BodyType.Dynamic;
                    shape = new PolygonShape();
                    shape.SetAsBox(0.5f, 0.5f);
                    box.FixedRotation = false;
                    // TODO: Need to detect shape and work out if we need to use fixedrotation

                    var fixture = new Fixture(box, shape)
                    {
                        CollisionMask = 2,
                        CollisionLayer = 2,
                        Hard = true,
                        Density = 1.0f,
                        Friction = 0.3f,
                    };

                    broadphase.CreateFixture(box, fixture);
                    physics.WakeBody(box);
                }
            }

            physics.WakeBody(ground);
        }

        private void CreateCircleStack(MapId mapId)
        {
            var physics = _ent.System<SharedPhysicsSystem>();

            var groundUid = _ent.SpawnEntity(null, new MapCoordinates(0, 0, mapId));
            var ground = _ent.AddComponent<PhysicsComponent>(groundUid);

            var horizontal = new EdgeShape(new Vector2(-40, 0), new Vector2(40, 0));
            var horizontalFixture = new Fixture(ground, horizontal)
            {
                CollisionLayer = 2,
                CollisionMask = 2,
                Hard = true
            };

            var broadphase = EntitySystem.Get<FixtureSystem>();
            broadphase.CreateFixture(ground, horizontalFixture);

            var vertical = new EdgeShape(new Vector2(10, 0), new Vector2(10, 10));
            var verticalFixture = new Fixture(ground, vertical)
            {
                CollisionLayer = 2,
                CollisionMask = 2,
                Hard = true
            };

            broadphase.CreateFixture(ground, verticalFixture);

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

                    box.BodyType = BodyType.Dynamic;
                    shape = new PhysShapeCircle {Radius = 0.5f};
                    box.FixedRotation = false;
                    // TODO: Need to detect shape and work out if we need to use fixedrotation

                    var fixture = new Fixture(box, shape)
                    {
                        CollisionMask = 2,
                        CollisionLayer = 2,
                        Hard = true,
                        Density = 5.0f,
                    };

                    broadphase.CreateFixture(box, fixture);
                    physics.WakeBody(box);
                }
            }

            physics.WakeBody(ground);
        }

        private void CreatePyramid(MapId mapId)
        {
            const byte count = 20;

            // Setup ground
            var physics = _ent.System<SharedPhysicsSystem>();
            var groundUid = _ent.SpawnEntity(null, new MapCoordinates(0, 0, mapId));
            var ground = _ent.AddComponent<PhysicsComponent>(groundUid);

            var horizontal = new EdgeShape(new Vector2(40, 0), new Vector2(-40, 0));
            var horizontalFixture = new Fixture(ground, horizontal)
            {
                CollisionLayer = 2,
                CollisionMask = 2,
                Hard = true
            };

            var broadphase = EntitySystem.Get<FixtureSystem>();
            broadphase.CreateFixture(ground, horizontalFixture);
            physics.WakeBody(ground);

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
                    var boxUid = _ent.SpawnEntity(null, new MapCoordinates(0, 0, mapId));
                    var box = _ent.AddComponent<PhysicsComponent>(boxUid);
                    box.BodyType = BodyType.Dynamic;
                    _ent.GetComponent<TransformComponent>(box.Owner).WorldPosition = y;
                    broadphase.CreateFixture(box,
                        new Fixture(box, shape) {
                        CollisionLayer = 2,
                        CollisionMask = 2,
                        Hard = true,
                        Density = 5.0f,
                    });
                    y += deltaY;

                    physics.WakeBody(box);
                }

                x += deltaX;
            }
        }

        private void CreateTumbler(MapId mapId)
        {
            var physics = _ent.System<SharedPhysicsSystem>();
            var broadphaseSystem = EntitySystem.Get<FixtureSystem>();

            var groundUid = _ent.SpawnEntity(null, new MapCoordinates(0f, 0f, mapId));
            var ground = _ent.AddComponent<PhysicsComponent>(groundUid);
            // Due to lookup changes fixtureless bodies are invalid, so
            var cShape = new PhysShapeCircle();
            broadphaseSystem.CreateFixture(ground, cShape);

            var bodyUid = _ent.SpawnEntity(null, new MapCoordinates(0f, 10f, mapId));
            var body = _ent.AddComponent<PhysicsComponent>(bodyUid);

            body.BodyType = BodyType.Dynamic;
            body.SleepingAllowed = false;
            body.FixedRotation = false;

            // TODO: Box2D just derefs, bleh shape structs someday
            var shape1 = new PolygonShape();
            shape1.SetAsBox(0.5f, 10.0f, new Vector2(10.0f, 0.0f), 0.0f);
            broadphaseSystem.CreateFixture(body, shape1, 20.0f, 2, 0);

            var shape2 = new PolygonShape();
            shape2.SetAsBox(0.5f, 10.0f, new Vector2(-10.0f, 0.0f), 0f);
            broadphaseSystem.CreateFixture(body, shape2, 20.0f, 2, 0);

            var shape3 = new PolygonShape();
            shape3.SetAsBox(10.0f, 0.5f, new Vector2(0.0f, 10.0f), 0f);
            broadphaseSystem.CreateFixture(body, shape3, 20.0f, 2, 0);

            var shape4 = new PolygonShape();
            shape4.SetAsBox(10.0f, 0.5f, new Vector2(0.0f, -10.0f), 0f);
            broadphaseSystem.CreateFixture(body, shape4, 20.0f, 2, 0);

            physics.WakeBody(ground);
            physics.WakeBody(body);
            var revolute = EntitySystem.Get<SharedJointSystem>().CreateRevoluteJoint(groundUid, bodyUid);
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
                    box.BodyType = BodyType.Dynamic;
                    box.FixedRotation = false;
                    var shape = new PolygonShape();
                    shape.SetAsBox(0.125f, 0.125f);
                    broadphaseSystem.CreateFixture(box, shape, 0.0625f, 2, 2);
                    physics.WakeBody(box);
                });
            }
        }
    }
}
