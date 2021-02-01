using System;
using System.Collections.Generic;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Dynamics.Contacts;

namespace Robust.Shared.Physics.Dynamics
{
    internal sealed class PhysicsIsland
    {
        // TODO: Cache the cvars
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;

        private float AngTolSqr => MathF.Pow(_configManager.GetCVar(CVars.AngularSleepTolerance), 2);

        private float LinTolSqr => MathF.Pow(_configManager.GetCVar(CVars.LinearSleepTolerance), 2);

        private ContactSolver _contactSolver = default!;

        public PhysicsComponent[] Bodies = Array.Empty<PhysicsComponent>();

        private Contact[] _contacts = Array.Empty<Contact>();

        // These are joint in box2d / derivatives
        private Vector2[] _linearVelocities = Array.Empty<Vector2>();
        private float[] _angularVelocities = Array.Empty<float>();

        private Vector2[] _positions = Array.Empty<Vector2>();
        private float[] _angles = Array.Empty<float>();

        /// <summary>
        ///     How many bodies we can fit in the island before needing to re-size.
        /// </summary>
        public int BodyCapacity { get; private set; }

        /// <summary>
        ///     How many bodies are in the island.
        /// </summary>
        public int BodyCount { get; private set; }

        /// <summary>
        ///     How many contacts we can fit in the island before needing to re-size.
        /// </summary>
        public int ContactCapacity { get; private set; }

        /// <summary>
        ///     How many contacts are in the island.
        /// </summary>
        public int ContactCount { get; private set; }

        public void Initialize()
        {
            IoCManager.InjectDependencies(this);
            _contactSolver = new ContactSolver();
            _contactSolver.Initialize();
        }

        public void Add(PhysicsComponent body)
        {
            body.IslandIndex = BodyCount;
            Bodies[BodyCount++] = body;
        }

        public void Add(Contact contact)
        {
            _contacts[ContactCount++] = contact;
        }

        public void Clear()
        {
            BodyCount = 0;
            ContactCount = 0;
        }

        public void Reset(int bodyCapacity, int contactCapacity)
        {
            BodyCapacity = bodyCapacity;
            BodyCount = 0;

            ContactCapacity = contactCapacity;
            ContactCount = 0;

            if (Bodies.Length < BodyCapacity)
            {
                Array.Resize(ref Bodies, BodyCapacity);
                Array.Resize(ref _linearVelocities, bodyCapacity);
                Array.Resize(ref _angularVelocities, bodyCapacity);
                Array.Resize(ref _positions, bodyCapacity);
                Array.Resize(ref _angles, bodyCapacity);
            }

            if (_contacts.Length < contactCapacity)
            {
                Array.Resize(ref _contacts, contactCapacity);
            }
        }

        public void Solve(float frameTime, float dtRatio, bool prediction)
        {
#if DEBUG
            var debugBodies = new List<PhysicsComponent>();
            for (var i = 0; i < BodyCount; i++)
            {
                debugBodies.Add(Bodies[i]);
            }

            IoCManager.Resolve<IEntityManager>().EventBus.RaiseEvent(EventSource.Local, new IslandSolveMessage(debugBodies));
#endif

            // TODO: This is probably suss given we're integrating before collisions?
            for (var i = 0; i < BodyCount; i++)
            {
                var body = Bodies[i];

                // In future we'll set these to existing
                // Didn't use the old variable names because they're hard to read
                var position = body.Owner.Transform.WorldPosition;
                var angle = (float) body.Owner.Transform.WorldRotation.Theta;
                var linearVelocity = body.LinearVelocity;
                var angularVelocity = body.AngularVelocity;

                // if the body cannot move, nothing to do here
                // TODO: Change anchored to just use static bodytype instead.
                if (body.CanMove())
                {
                    foreach (var controller in body.GetControllers())
                    {
                        linearVelocity += controller.LinearVelocity * frameTime;
                        linearVelocity += controller.Impulse * body.InvMass * frameTime;
                    }

                    angularVelocity += frameTime * body.InvI * body.Torque;

                    // Process frictional forces
                    // TODO: Might change how TileFriction works here, idk. The overall formula is from FPE regardless.
                    var tileFriction = GetTileFriction(body);

                    linearVelocity *= Math.Clamp(1.0f - frameTime * MathF.Sqrt(body.LinearDamping * tileFriction), 0.0f, 1.0f);
                    angularVelocity *= Math.Clamp(1.0f - frameTime * MathF.Sqrt(body.AngularDamping * tileFriction), 0.0f, 1.0f);
                }

                _positions[i] = position;
                _angles[i] = angle;
                _linearVelocities[i] = linearVelocity;
                _angularVelocities[i] = angularVelocity;
            }

            // Pass the data into the solver
            _contactSolver.Reset(dtRatio, ContactCount, _contacts, _linearVelocities, _angularVelocities, _positions, _angles);

            _contactSolver.InitializeVelocityConstraints();

            if (_configManager.GetCVar(CVars.WarmStarting))
            {
                _contactSolver.WarmStart();
            }

            // TODO: Joint inits

            // Velocity solver
            for (var i = 0; i < _configManager.GetCVar(CVars.VelocityIterations); i++)
            {
                // TODO: Joints here

                _contactSolver.SolveVelocityConstraints();
            }

            // Store for warm starting.
            _contactSolver.StoreImpulses();

            var maxLinVelocity = _configManager.GetCVar(CVars.MaxLinVelocity);
            var maxAngVelocity = _configManager.GetCVar(CVars.MaxAngVelocity);

            // Integrate positions
            for (var i = 0; i < BodyCount; i++)
            {
                var linearVelocity = _linearVelocities[i];
                var angularVelocity = _angularVelocities[i];

                var position = _positions[i];
                var angle = _angles[i];

                var translation = linearVelocity * frameTime;
                if (Vector2.Dot(translation, translation) > maxLinVelocity)
                {
                    var ratio = 4.0f / translation.Length;
                    linearVelocity *= ratio;
                }

                var rotation = angularVelocity * frameTime;
                if (rotation * rotation > maxAngVelocity)
                {
                    var ratio = 2.0f / MathF.Abs(rotation);
                    angularVelocity *= ratio;
                }

                // Integrate
                position += linearVelocity * frameTime;
                angle += angularVelocity * frameTime;

                _linearVelocities[i] = linearVelocity;
                _angularVelocities[i] = angularVelocity;

                _positions[i] = position;
                _angles[i] = angle;
            }

            var positionSolved = false;

            for (var i = 0; i < _configManager.GetCVar(CVars.PositionIterations); i++)
            {
                var contactsOkay = _contactSolver.SolvePositionConstraints();

                // TODO: Joints here

                if (contactsOkay)
                {
                    positionSolved = true;
                    break;
                }
            }

            // Update data on bodies by copying the buffers back
            for (var i = 0; i < BodyCount; i++)
            {
                var body = Bodies[i];

                // TODO: Do we need this? We shouldn't...?
                if (body.BodyType == BodyType.Static) continue;

                /*
                 * Handle new velocity
                 */

                var linearVelocity = _linearVelocities[i];

                if (linearVelocity != Vector2.Zero)
                {
                    if (body.Owner.IsInContainer())
                    {
                        var relayEntityMoveMessage = new RelayMovementEntityMessage(body.Owner);
                        body.Owner.Transform.Parent!.Owner.SendMessage(body.Owner.Transform, relayEntityMoveMessage);
                    }
                }

                /*
                 * Handle new position
                 */
                var bodyPos = _positions[i];

                // Change parent if necessary
                if (!body.Owner.IsInContainer())
                {
                    // This shoouullddnnn'''tt de-parent anything in a container because none of that should have physics applied to it.
                    if (_mapManager.TryFindGridAt(body.Owner.Transform.MapID, bodyPos, out var grid) &&
                        grid.GridEntityId.IsValid() &&
                        grid.GridEntityId != body.Owner.Uid)
                    {
                        if (grid.GridEntityId != body.Owner.Transform.ParentUid)
                            body.Owner.Transform.AttachParent(body.Owner.EntityManager.GetEntity(grid.GridEntityId));
                    }
                    else
                    {
                        body.Owner.Transform.AttachParent(_mapManager.GetMapEntity(body.Owner.Transform.MapID));
                    }
                }

                // body.Sweep.Center = _positions[i];
                // body.Sweep.Angle = _angles[i];

                body.Owner.Transform.WorldPosition = bodyPos;
                // TODO: We need some override for players as this will go skewiff.
                body.Owner.Transform.WorldRotation = _angles[i];

                body.LinearVelocity = _linearVelocities[i];
                body.AngularVelocity = _angularVelocities[i];
            }

            // TODO: Cache rather than GetCVar
            // Sleep bodies if needed. Prediction won't accumulate sleep-time for bodies.
            if (_configManager.GetCVar(CVars.SleepAllowed) && !prediction)
            {
                var minSleepTime = float.MaxValue;

                for (var i = 0; i < BodyCount; i++)
                {
                    var body = Bodies[i];

                    if (body.BodyType == BodyType.Static)
                        continue;

                    if (!body.SleepingAllowed ||
                        body.AngularVelocity * body.AngularVelocity > AngTolSqr ||
                        Vector2.Dot(body.LinearVelocity, body.LinearVelocity) > LinTolSqr)
                    {
                        body.SleepTime = 0.0f;
                        minSleepTime = 0.0f;
                    }
                    else
                    {
                        body.SleepTime += frameTime;
                        minSleepTime = MathF.Min(minSleepTime, body.SleepTime);
                    }
                }

                if (minSleepTime >= _configManager.GetCVar(CVars.TimeToSleep) && positionSolved)
                {
                    for (var i = 0; i < BodyCount; i++)
                    {
                        var body = Bodies[i];
                        body.Awake = false;
                    }
                }
            }
        }

        private float GetTileFriction(IPhysicsComponent body)
        {
            if (!body.OnGround)
                return 0.0f;

            var location = body.Owner.Transform;
            var grid = _mapManager.GetGrid(location.Coordinates.GetGridId(_entityManager));
            var tile = grid.GetTileRef(location.Coordinates);
            var tileDef = _tileDefinitionManager[tile.Tile.TypeId];
            return tileDef.Friction;
        }
    }
}
