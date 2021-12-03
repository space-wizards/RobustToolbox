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
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Dynamics.Joints;
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
    public class TestbedCommand : IConsoleCommand
    {
        public string Command => "testbed";
        public string Description => "Loads a physics testbed on the specified map.";
        public string Help => $"{Command} <mapid> <test>";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteLine("Require 2 args for testbed!");
                return;
            }

            var mapManager = IoCManager.Resolve<IMapManager>();

            if (!int.TryParse(args[0], out var mapInt))
            {
                shell.WriteLine($"Unable to parse map {args[0]}");
                return;
            }

            var mapId = new MapId(mapInt);
            if (!mapManager.MapExists(mapId))
            {
                shell.WriteLine("Unable to find map {mapId}");
                return;
            }

            if (shell.Player == null)
            {
                shell.WriteLine("No player found");
                return;
            }

            var player = (IPlayerSession) shell.Player;

            switch (args[1])
            {
                case "boxstack":
                    SetupPlayer(mapId, shell, player, mapManager);
                    CreateBoxStack(mapId);
                    break;
                case "circlestack":
                    SetupPlayer(mapId, shell, player, mapManager);
                    CreateCircleStack(mapId);
                    break;
                case "pyramid":
                    SetupPlayer(mapId, shell, player, mapManager);
                    CreatePyramid(mapId);
                    break;
                case "tumbler":
                    SetupPlayer(mapId, shell, player, mapManager);
                    CreateTumbler(mapId);
                    break;
                default:
                    shell.WriteLine($"testbed {args[0]} not found!");
                    return;
            }

            shell.WriteLine($"Testbed on map {mapId}");
        }

        private void SetupPlayer(MapId mapId, IConsoleShell shell, IPlayerSession? player, IMapManager mapManager)
        {
            if (mapId == MapId.Nullspace) return;
            var pauseManager = IoCManager.Resolve<IPauseManager>();
            pauseManager.SetMapPaused(mapId, false);
            IoCManager.Resolve<IMapManager>().GetMapEntity(mapId).GetComponent<SharedPhysicsMapComponent>().Gravity = new Vector2(0, -9.8f);

            return;
        }

        private void CreateBoxStack(MapId mapId)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();

            IEntity tempQualifier = entityManager.SpawnEntity(null, new MapCoordinates(0, 0, mapId));
            var ground = IoCManager.Resolve<IEntityManager>().AddComponent<PhysicsComponent>(tempQualifier);

            var horizontal = new EdgeShape(new Vector2(-20, 0), new Vector2(20, 0));
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
            PolygonShape shape;

            for (var j = 0; j < columnCount; j++)
            {
                for (var i = 0; i < rowCount; i++)
                {
                    var x = 0.0f;

                    IEntity tempQualifier1 = entityManager.SpawnEntity(null,
                        new MapCoordinates(new Vector2(xs[j] + x, 0.55f + 2.1f * i), mapId));
                    var box = IoCManager.Resolve<IEntityManager>().AddComponent<PhysicsComponent>(tempQualifier1);

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
                    };

                    broadphase.CreateFixture(box, fixture);
                }
            }
        }

        private void CreateCircleStack(MapId mapId)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();

            IEntity tempQualifier = entityManager.SpawnEntity(null, new MapCoordinates(0, 0, mapId));
            var ground = IoCManager.Resolve<IEntityManager>().AddComponent<PhysicsComponent>(tempQualifier);

            var horizontal = new EdgeShape(new Vector2(-20, 0), new Vector2(20, 0));
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

                    IEntity tempQualifier1 = entityManager.SpawnEntity(null,
                        new MapCoordinates(new Vector2(xs[j] + x, 0.55f + 2.1f * i), mapId));
                    var box = IoCManager.Resolve<IEntityManager>().AddComponent<PhysicsComponent>(tempQualifier1);

                    box.BodyType = BodyType.Dynamic;
                    shape = new PhysShapeCircle {Radius = 0.5f};
                    box.FixedRotation = false;
                    // TODO: Need to detect shape and work out if we need to use fixedrotation

                    var fixture = new Fixture(box, shape)
                    {
                        CollisionMask = 2,
                        CollisionLayer = 2,
                        Hard = true,
                    };

                    broadphase.CreateFixture(box, fixture);
                }
            }
        }

        private void CreatePyramid(MapId mapId)
        {
            const byte count = 20;

            // Setup ground
            var entityManager = IoCManager.Resolve<IEntityManager>();
            IEntity tempQualifier = entityManager.SpawnEntity(null, new MapCoordinates(0, 0, mapId));
            var ground = IoCManager.Resolve<IEntityManager>().AddComponent<PhysicsComponent>(tempQualifier);

            var horizontal = new EdgeShape(new Vector2(-40, 0), new Vector2(40, 0));
            var horizontalFixture = new Fixture(ground, horizontal)
            {
                CollisionLayer = 2,
                CollisionMask = 2,
                Hard = true
            };

            var broadphase = EntitySystem.Get<FixtureSystem>();
            broadphase.CreateFixture(ground, horizontalFixture);

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
                    IEntity tempQualifier1 = entityManager.SpawnEntity(null, new MapCoordinates(0, 0, mapId));
                    var box = IoCManager.Resolve<IEntityManager>().AddComponent<PhysicsComponent>(tempQualifier1);
                    box.BodyType = BodyType.Dynamic;
                    box.Owner.Transform.WorldPosition = y;
                    broadphase.CreateFixture(box,
                        new Fixture(box, shape) {
                        CollisionLayer = 2,
                        CollisionMask = 2,
                        Hard = true,
                        Mass = 5.0f,
                    });
                    y += deltaY;
                }

                x += deltaX;
            }
        }

        private void CreateTumbler(MapId mapId)
        {
            var broadphaseSystem = EntitySystem.Get<FixtureSystem>();
            var entityManager = IoCManager.Resolve<IEntityManager>();

            var groundUid = entityManager.SpawnEntity(null, new MapCoordinates(0f, 0f, mapId)).Uid;
            var ground = entityManager.AddComponent<PhysicsComponent>(groundUid);

            var bodyUid = entityManager.SpawnEntity(null, new MapCoordinates(0f, 10f, mapId)).Uid;
            var body = entityManager.AddComponent<PhysicsComponent>(bodyUid);

            body.BodyType = BodyType.Dynamic;
            body.SleepingAllowed = false;
            body.FixedRotation = false;

            // TODO: Box2D just derefs, bleh shape structs someday
            var shape1 = new PolygonShape();
            shape1.SetAsBox(0.5f, 10.0f, new Vector2(10.0f, 0.0f), 0.0f);
            broadphaseSystem.CreateFixture(body, shape1, 20.0f);

            var shape2 = new PolygonShape();
            shape2.SetAsBox(0.5f, 10.0f, new Vector2(-10.0f, 0.0f), 0f);
            broadphaseSystem.CreateFixture(body, shape2, 20.0f);

            var shape3 = new PolygonShape();
            shape3.SetAsBox(10.0f, 0.5f, new Vector2(0.0f, 10.0f), 0f);
            broadphaseSystem.CreateFixture(body, shape3, 20.0f);

            var shape4 = new PolygonShape();
            shape4.SetAsBox(10.0f, 0.5f, new Vector2(0.0f, -10.0f), 0f);
            broadphaseSystem.CreateFixture(body, shape4, 20.0f);

            foreach (var fixture in body.Fixtures)
            {
                fixture.CollisionLayer = 2;
            }

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
            var mapManager = IoCManager.Resolve<IMapManager>();

            for (var i = 0; i < count; i++)
            {
                Timer.Spawn(i * 20, () =>
                {
                    if (!mapManager.MapExists(mapId)) return;
                    var ent = entityManager.SpawnEntity(null, new MapCoordinates(0f, 10f, mapId)).Uid;
                    var box = entityManager.AddComponent<PhysicsComponent>(ent);
                    box.BodyType = BodyType.Dynamic;
                    box.FixedRotation = false;
                    var shape = new PolygonShape();
                    shape.SetAsBox(0.125f, 0.125f);
                    broadphaseSystem.CreateFixture(box, shape, 0.0625f);
                    box.Fixtures[0].CollisionMask = 2;
                    box.Fixtures[0].CollisionLayer = 2;
                });
            }
        }
    }
}
