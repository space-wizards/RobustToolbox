/*
Microsoft Permissive License (Ms-PL)

This license governs use of the accompanying software. If you use the software, you accept this license.
If you do not accept the license, do not use the software.

1. Definitions
The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under
U.S. copyright law.
A "contribution" is the original software, or any additions or changes to the software.
A "contributor" is any person that distributes its contribution under this license.
"Licensed patents" are a contributor's patent claims that read directly on its contribution.

2. Grant of Rights
(A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3,
each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution,
prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
(B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3,
each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to
make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or
derivative works of the contribution in the software.

3. Conditions and Limitations
(A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
(B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software,
your patent license from such contributor to the software ends automatically.
(C) If you distribute any portion of the software, you must retain all copyright, patent, trademark,
and attribution notices that are present in the software.
(D) If you distribute any portion of the software in source code form, you may do so only under this license by
including a complete copy of this license with your distribution.
If you distribute any portion of the software in compiled or object code form, you may only do so under a license that
complies with this license.
(E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees or conditions.
You may have additional consumer rights under your local laws which this license cannot change.
To the extent permitted under your local laws, the contributors exclude the implied warranties of
merchantability, fitness for a particular purpose and non-infringement.
*/

using System;
using System.Collections.Generic;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Dynamics.Contacts;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Dynamics
{
/*
 * These comments scabbed directly from Box2D and the licence applies to them.
 */

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

/*
Position Correction Notes
=========================
I tried the several algorithms for position correction of the 2D revolute joint.
I looked at these systems:
- simple pendulum (1m diameter sphere on massless 5m stick) with initial angular velocity of 100 rad/s.
- suspension bridge with 30 1m long planks of length 1m.
- multi-link chain with 30 1m long links.
Here are the algorithms:
Baumgarte - A fraction of the position error is added to the velocity error. There is no
separate position solver.
Pseudo Velocities - After the velocity solver and position integration,
the position error, Jacobian, and effective mass are recomputed. Then
the velocity constraints are solved with pseudo velocities and a fraction
of the position error is added to the pseudo velocity error. The pseudo
velocities are initialized to zero and there is no warm-starting. After
the position solver, the pseudo velocities are added to the positions.
This is also called the First Order World method or the Position LCP method.
Modified Nonlinear Gauss-Seidel (NGS) - Like Pseudo Velocities except the
position error is re-computed for each constraint and the positions are updated
after the constraint is solved. The radius vectors (aka Jacobians) are
re-computed too (otherwise the algorithm has horrible instability). The pseudo
velocity states are not needed because they are effectively zero at the beginning
of each iteration. Since we have the current position error, we allow the
iterations to terminate early if the error becomes smaller than b2_linearSlop.
Full NGS or just NGS - Like Modified NGS except the effective mass are re-computed
each time a constraint is solved.
Here are the results:
Baumgarte - this is the cheapest algorithm but it has some stability problems,
especially with the bridge. The chain links separate easily close to the root
and they jitter as they struggle to pull together. This is one of the most common
methods in the field. The big drawback is that the position correction artificially
affects the momentum, thus leading to instabilities and false bounce. I used a
bias factor of 0.2. A larger bias factor makes the bridge less stable, a smaller
factor makes joints and contacts more spongy.
Pseudo Velocities - the is more stable than the Baumgarte method. The bridge is
stable. However, joints still separate with large angular velocities. Drag the
simple pendulum in a circle quickly and the joint will separate. The chain separates
easily and does not recover. I used a bias factor of 0.2. A larger value lead to
the bridge collapsing when a heavy cube drops on it.
Modified NGS - this algorithm is better in some ways than Baumgarte and Pseudo
Velocities, but in other ways it is worse. The bridge and chain are much more
stable, but the simple pendulum goes unstable at high angular velocities.
Full NGS - stable in all tests. The joints display good stiffness. The bridge
still sags, but this is better than infinite forces.
Recommendations
Pseudo Velocities are not really worthwhile because the bridge and chain cannot
recover from joint separation. In other cases the benefit over Baumgarte is small.
Modified NGS is not a robust method for the revolute joint due to the violent
instability seen in the simple pendulum. Perhaps it is viable with other constraint
types, especially scalar constraints where the effective mass is a scalar.
This leaves Baumgarte and Full NGS. Baumgarte has small, but manageable instabilities
and is very fast. I don't think we can escape Baumgarte, especially in highly
demanding cases where high constraint fidelity is not needed.
Full NGS is robust and easy on the eyes. I recommend this as an option for
higher fidelity simulation and certainly for suspension bridges and long chains.
Full NGS might be a good choice for ragdolls, especially motorized ragdolls where
joint separation can be problematic. The number of NGS iterations can be reduced
for better performance without harming robustness much.
Each joint in a can be handled differently in the position solver. So I recommend
a system where the user can select the algorithm on a per joint basis. I would
probably default to the slower Full NGS and let the user select the faster
Baumgarte method in performance critical scenarios.
*/

/*
Cache Performance
The Box2D solvers are dominated by cache misses. Data structures are designed
to increase the number of cache hits. Much of misses are due to random access
to body data. The constraint structures are iterated over linearly, which leads
to few cache misses.
The bodies are not accessed during iteration. Instead read only data, such as
the mass values are stored with the constraints. The mutable data are the constraint
impulses and the bodies velocities/positions. The impulses are held inside the
constraint structures. The body velocities/positions are held in compact, temporary
arrays to increase the number of cache hits. Linear and angular velocity are
stored in a single array since multiple arrays lead to multiple misses.
*/
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

        public void Solve(Vector2 gravity, float frameTime, float dtRatio, float invDt, bool prediction)
        {
#if DEBUG
            var debugBodies = new List<PhysicsComponent>();
            for (var i = 0; i < BodyCount; i++)
            {
                debugBodies.Add(Bodies[i]);
            }

            IoCManager.Resolve<IEntityManager>().EventBus.RaiseEvent(EventSource.Local, new IslandSolveMessage(debugBodies));
#endif

            for (var i = 0; i < BodyCount; i++)
            {
                var body = Bodies[i];

                // In future we'll set these to existing
                // Didn't use the old variable names because they're hard to read
                var position = body.Owner.Transform.WorldPosition;
                // DebugTools.Assert(!float.IsNaN(position.X) && !float.IsNaN(position.Y));
                var angle = (float) body.Owner.Transform.WorldRotation.Theta;
                var linearVelocity = body.LinearVelocity;
                var angularVelocity = body.AngularVelocity;

                // if the body cannot move, nothing to do here
                if (body.BodyType == BodyType.Dynamic)
                {
                    if (body.IgnoreGravity)
                        linearVelocity += body.Force * frameTime * body.InvMass;
                    else
                        linearVelocity += (gravity * body.GravityScale + body.Force * body.InvMass) * frameTime;

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
                    var ratio = maxLinVelocity / translation.Length;
                    linearVelocity *= ratio;
                }

                var rotation = angularVelocity * frameTime;
                if (rotation * rotation > maxAngVelocity)
                {
                    var ratio = maxAngVelocity / MathF.Abs(rotation);
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

                if (contactsOkay && jointsOkay)
                {
                    positionSolved = true;
                    break;
                }
            }

            // Update data on bodies by copying the buffers back
            for (var i = 0; i < BodyCount; i++)
            {
                var body = Bodies[i];

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
                        // Also this may deparent if 2 entities are parented but not using containers so fix that
                        if (grid.GridEntityId != body.Owner.Transform.ParentUid)
                        {
                            body.Owner.Transform.AttachParent(body.Owner.EntityManager.GetEntity(grid.GridEntityId));
                        }
                    }
                    else
                    {
                        body.Owner.Transform.AttachParent(_mapManager.GetMapEntity(body.Owner.Transform.MapID));
                    }
                }

                // body.Sweep.Center = _positions[i];
                // body.Sweep.Angle = _angles[i];

                // DebugTools.Assert(!float.IsNaN(bodyPos.X) && !float.IsNaN(bodyPos.Y));

                body.Owner.Transform.WorldPosition = bodyPos;
                body.Owner.Transform.WorldRotation = _angles[i];

                body.LinearVelocity = _linearVelocities[i];
                body.AngularVelocity = _angularVelocities[i];
            }

            // TODO: Cache rather than GetCVar
            // Sleep bodies if needed. Prediction won't accumulate sleep-time for bodies.
            if (!prediction && _configManager.GetCVar(CVars.SleepAllowed))
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
