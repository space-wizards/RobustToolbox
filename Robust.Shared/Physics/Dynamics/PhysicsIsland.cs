using System;
using System.Collections.Generic;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Dynamics.Contacts;
using Robust.Shared.Physics.Dynamics.Joints;

namespace Robust.Shared.Physics.Dynamics
{
    internal sealed class PhysicsIsland
    {
        // TODO: Cache the cvars
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;

        private ContactSolver _contactSolver = default!;

        private float AngTolSqr => MathF.Pow(_configManager.GetCVar(CVars.AngularSleepTolerance), 2);

        private float LinTolSqr => MathF.Pow(_configManager.GetCVar(CVars.LinearSleepTolerance), 2);

        public PhysicsComponent[] Bodies = Array.Empty<PhysicsComponent>();
        private Contact[] _contacts = Array.Empty<Contact>();
        private Joint[] _joints = Array.Empty<Joint>();

        // These are joint in box2d / derivatives
        private Vector2[] _linearVelocities = Array.Empty<Vector2>();
        private float[] _angularVelocities = Array.Empty<float>();

        private Vector2[] _positions = Array.Empty<Vector2>();
        private float[] _angles = Array.Empty<float>();

        internal SolverData _solverData = new();

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

        /// <summary>
        ///     How many joints we can fit in the island before needing to re-size.
        /// </summary>
        public int JointCapacity { get; private set; }

        /// <summary>
        ///     How many joints are in the island.
        /// </summary>
        public int JointCount { get; private set; }

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

        public void Add(Joint joint)
        {
            _joints[JointCount++] = joint;
        }

        public void Clear()
        {
            BodyCount = 0;
            ContactCount = 0;
            JointCount = 0;
        }

        /*
         * Look there's a whole lot of stuff going on around here but all you need to know is it's trying to avoid
         * allocations where possible so it does a whole lot of passing data around and using arrays.
         */

        public void Reset(int bodyCapacity, int contactCapacity, int jointCapacity)
        {
            BodyCapacity = bodyCapacity;
            BodyCount = 0;

            ContactCapacity = contactCapacity;
            ContactCount = 0;

            JointCapacity = jointCapacity;
            JointCount = 0;

            if (Bodies.Length < bodyCapacity)
            {
                Array.Resize(ref Bodies, bodyCapacity);
                Array.Resize(ref _linearVelocities, bodyCapacity);
                Array.Resize(ref _angularVelocities, bodyCapacity);
                Array.Resize(ref _positions, bodyCapacity);
                Array.Resize(ref _angles, bodyCapacity);
            }

            if (_contacts.Length < contactCapacity)
            {
                Array.Resize(ref _contacts, contactCapacity * 2);
            }

            if (_joints.Length < jointCapacity)
            {
                Array.Resize(ref _joints, jointCapacity * 2);
            }
        }

        public void Solve(float frameTime, float dtRatio, float invDt, bool prediction)
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
                if (body.BodyType == BodyType.Dynamic)
                {
                    // TODO: Look at FullWalkMove under https://github.com/ValveSoftware/source-sdk-2013/blob/master/sp/src/game/shared/gamemovement.cpp#L1822
                    linearVelocity += body.Force * frameTime;
                    angularVelocity += frameTime * body.InvI * body.Torque;

                    linearVelocity *= Math.Clamp(1.0f - frameTime * body.LinearDamping, 0.0f, 1.0f);
                    angularVelocity *= Math.Clamp(1.0f - frameTime * body.AngularDamping, 0.0f, 1.0f);
                }

                _positions[i] = position;
                _angles[i] = angle;
                _linearVelocities[i] = linearVelocity;
                _angularVelocities[i] = angularVelocity;
            }

            // TODO: Do these up front of the world step.
            _solverData.FrameTime = frameTime;
            _solverData.DtRatio = dtRatio;
            _solverData.InvDt = invDt;

            _solverData.LinearVelocities = _linearVelocities;
            _solverData.AngularVelocities = _angularVelocities;
            _solverData.Positions = _positions;
            _solverData.Angles = _angles;

            // Pass the data into the solver
            _contactSolver.Reset(_solverData, ContactCount, _contacts);

            _contactSolver.InitializeVelocityConstraints();

            if (_configManager.GetCVar(CVars.WarmStarting))
            {
                _contactSolver.WarmStart();
            }

            for (var i = 0; i < JointCount; i++)
            {
                var joint = _joints[i];
                if (!joint.Enabled) continue;
                joint.InitVelocityConstraints(_solverData);
            }

            // Velocity solver
            for (var i = 0; i < _configManager.GetCVar(CVars.VelocityIterations); i++)
            {
                for (var j = 0; j < JointCount; ++j)
                {
                    Joint joint = _joints[j];

                    if (!joint.Enabled)
                        continue;

                    joint.SolveVelocityConstraints(_solverData);
                    joint.Validate(invDt);
                }

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
                var jointsOkay = true;

                for (int j = 0; j < JointCount; ++j)
                {
                    Joint joint = _joints[j];

                    if (!joint.Enabled)
                        continue;

                    bool jointOkay = joint.SolvePositionConstraints(_solverData);

                    jointsOkay = jointsOkay && jointOkay;
                }

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

    /// <summary>
    ///     Easy way of passing around the data required for the contact solver.
    /// </summary>
    internal sealed class SolverData
    {
        public float FrameTime { get; set; }
        public float DtRatio { get; set; }
        public float InvDt { get; set; }

        public Vector2[] LinearVelocities { get; set; } = default!;
        public float[] AngularVelocities { get; set; } = default!;

        public Vector2[] Positions { get; set; } = default!;
        public float[] Angles { get; set; } = default!;
    }
}
