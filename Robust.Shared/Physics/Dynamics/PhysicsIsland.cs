/*
* Farseer Physics Engine:
* Copyright (c) 2012 Ian Qvist
*
* Original source Box2D:
* Copyright (c) 2006-2011 Erin Catto http://www.box2d.org
*
* This software is provided 'as-is', without any express or implied
* warranty.  In no event will the authors be held liable for any damages
* arising from the use of this software.
* Permission is granted to anyone to use this software for any purpose,
* including commercial applications, and to alter it and redistribute it
* freely, subject to the following restrictions:
* 1. The origin of this software must not be misrepresented; you must not
* claim that you wrote the original software. If you use this software
* in a product, an acknowledgment in the product documentation would be
* appreciated but is not required.
* 2. Altered source versions must be plainly marked as such, and must not be
* misrepresented as being the original software.
* 3. This notice may not be removed or altered from any source distribution.
*/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Dynamics.Contacts;
using Robust.Shared.Physics.Dynamics.Joints;

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
public sealed class PhysicsIsland
    {
        [Dependency] private readonly IConfigurationManager _configManager = default!;
#if DEBUG
        [Dependency] private readonly IEntityManager _entityManager = default!;
        private List<IPhysBody> _debugBodies = new(8);
#endif

        private ContactSolver _contactSolver = default!;

        private float _angTolSqr;
        private float _linTolSqr;
        private bool _warmStarting;
        private int _velocityIterations;
        private float _maxLinearVelocity;
        private float _maxAngularVelocity;
        private int _positionIterations;
        private bool _sleepAllowed;  // BONAFIDE MONAFIED
        private float _timeToSleep;
        private bool _positionSolved;

        public IPhysBody[] Bodies = Array.Empty<IPhysBody>();
        private Contact[] _contacts = Array.Empty<Contact>();
        private Joint[] _joints = Array.Empty<Joint>();

        // These are joint in box2d / derivatives
        private Vector2[] _linearVelocities = Array.Empty<Vector2>();
        private float[] _angularVelocities = Array.Empty<float>();

        private Vector2[] _positions = Array.Empty<Vector2>();
        private float[] _angles = Array.Empty<float>();

        internal SolverData SolverData = new();

        private const int BodyIncrease = 8;
        private const int ContactIncrease = 4;
        private const int JointIncrease = 4;
        private const int ReadMultithreadThreshold = 32;

        /// <summary>
        /// Do we apply special sleeping for this island given it has no contacts and joints?
        /// </summary>
        public bool LoneIsland { get; set; } = false;

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

            // Values
            _angTolSqr = MathF.Pow(_configManager.GetCVar(CVars.AngularSleepTolerance), 2);
            _configManager.OnValueChanged(CVars.AngularSleepTolerance, value => _angTolSqr = MathF.Pow(value, 2));

            _linTolSqr = MathF.Pow(_configManager.GetCVar(CVars.LinearSleepTolerance), 2);
            _configManager.OnValueChanged(CVars.LinearSleepTolerance, value => _linTolSqr = MathF.Pow(value, 2));

            _warmStarting = _configManager.GetCVar(CVars.WarmStarting);
            _configManager.OnValueChanged(CVars.WarmStarting, value => _warmStarting = value);

            _velocityIterations = _configManager.GetCVar(CVars.VelocityIterations);
            _configManager.OnValueChanged(CVars.VelocityIterations, value => _velocityIterations = value);

            _maxLinearVelocity = _configManager.GetCVar(CVars.MaxLinVelocity);
            _configManager.OnValueChanged(CVars.MaxLinVelocity, value => _maxLinearVelocity = value);

            _maxAngularVelocity = _configManager.GetCVar(CVars.MaxAngVelocity);
            _configManager.OnValueChanged(CVars.MaxAngVelocity, value => _maxAngularVelocity = value);

            _positionIterations = _configManager.GetCVar(CVars.PositionIterations);
            _configManager.OnValueChanged(CVars.PositionIterations, value => _positionIterations = value);

            _sleepAllowed = _configManager.GetCVar(CVars.SleepAllowed);
            _configManager.OnValueChanged(CVars.SleepAllowed, value => _sleepAllowed = value);

            _timeToSleep = _configManager.GetCVar(CVars.TimeToSleep);
            _configManager.OnValueChanged(CVars.TimeToSleep, value => _timeToSleep = value);
        }

        public void Append(List<IPhysBody> bodies, List<Contact> contacts, List<Joint> joints)
        {
            Resize(BodyCount + bodies.Count, ContactCount + contacts.Count, JointCount + joints.Count);
            foreach (var body in bodies)
            {
                Add(body);
            }

            foreach (var contact in contacts)
            {
                Add(contact);
            }

            foreach (var joint in joints)
            {
                Add(joint);
            }
        }

        public void Add(IPhysBody body)
        {
            throw new Exception("Parallel islands issue here");
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
        public void Resize(int bodyCount, int contactCount, int jointCount)
        {
            BodyCapacity = Math.Max(bodyCount, Bodies.Length);
            ContactCapacity = Math.Max(contactCount, _contacts.Length);
            JointCapacity = Math.Max(jointCount, _joints.Length);

            if (Bodies.Length < BodyCapacity)
            {
                BodyCapacity = BodyIncrease * (int) MathF.Ceiling(BodyCapacity / (float) BodyIncrease);
                Array.Resize(ref Bodies, BodyCapacity);
                Array.Resize(ref _linearVelocities, BodyCapacity);
                Array.Resize(ref _angularVelocities, BodyCapacity);
                Array.Resize(ref _positions, BodyCapacity);
                Array.Resize(ref _angles, BodyCapacity);
            }

            if (_contacts.Length < ContactCapacity)
            {
                ContactCapacity = ContactIncrease * (int) MathF.Ceiling(ContactCapacity / (float) ContactIncrease);
                Array.Resize(ref _contacts, ContactCapacity * 2);
            }

            if (_joints.Length < JointCapacity)
            {
                JointCapacity = JointIncrease * (int) MathF.Ceiling(JointCapacity / (float) JointIncrease);
                Array.Resize(ref _joints, JointCapacity * 2);
            }
        }

        /// <summary>
        ///     Go through all the bodies in this island and solve.
        /// </summary>
        /// <param name="gravity"></param>
        /// <param name="frameTime"></param>
        /// <param name="dtRatio"></param>
        /// <param name="invDt"></param>
        /// <param name="prediction"></param>
        /// <param name="deferredUpdates">Add any transform updates to a deferred list</param>
        public void Solve(Vector2 gravity, float frameTime, float dtRatio, float invDt)
        {
#if DEBUG
            _debugBodies.Clear();
            for (var i = 0; i < BodyCount; i++)
            {
                _debugBodies.Add(Bodies[i]);
            }

            _entityManager.EventBus.RaiseEvent(EventSource.Local, new IslandSolveMessage(_debugBodies));
#endif

            _positionSolved = false;

            if (BodyCount > ReadMultithreadThreshold * 4)
            {
                // Deduct 1 for networking thread I guess?
                var batches = Math.Min((int) MathF.Floor((float) BodyCount / ReadMultithreadThreshold), Math.Max(1, Environment.ProcessorCount - 1));
                var batchSize = (int) MathF.Ceiling((float) BodyCount / batches);

                Parallel.For(0, batches, i =>
                {
                    var start = i * batchSize;
                    var end = Math.Min(start + batchSize, BodyCount);
                    ReadBodies(start, end, frameTime, gravity);
                });
            }
            else
            {
                ReadBodies(0, BodyCount, frameTime, gravity);
            }

            // TODO: Do these up front of the world step.
            SolverData.FrameTime = frameTime;
            SolverData.DtRatio = dtRatio;
            SolverData.InvDt = invDt;

            SolverData.LinearVelocities = _linearVelocities;
            SolverData.AngularVelocities = _angularVelocities;
            SolverData.Positions = _positions;
            SolverData.Angles = _angles;

            // Pass the data into the solver
            _contactSolver.Reset(SolverData, ContactCount, _contacts);

            _contactSolver.InitializeVelocityConstraints();

            if (_warmStarting)
            {
                _contactSolver.WarmStart();
            }

            for (var i = 0; i < JointCount; i++)
            {
                var joint = _joints[i];
                if (!joint.Enabled) continue;
                joint.InitVelocityConstraints(SolverData);
            }

            // Velocity solver
            for (var i = 0; i < _velocityIterations; i++)
            {
                for (var j = 0; j < JointCount; ++j)
                {
                    Joint joint = _joints[j];

                    if (!joint.Enabled)
                        continue;

                    joint.SolveVelocityConstraints(SolverData);
                    joint.Validate(invDt);
                }

                _contactSolver.SolveVelocityConstraints();
            }

            // Store for warm starting.
            _contactSolver.StoreImpulses();

            // Integrate positions
            for (var i = 0; i < BodyCount; i++)
            {
                var linearVelocity = _linearVelocities[i];
                var angularVelocity = _angularVelocities[i];

                var position = _positions[i];
                var angle = _angles[i];

                var translation = linearVelocity * frameTime;
                if (Vector2.Dot(translation, translation) > _maxLinearVelocity)
                {
                    var ratio = _maxLinearVelocity / translation.Length;
                    linearVelocity *= ratio;
                }

                var rotation = angularVelocity * frameTime;
                if (rotation * rotation > _maxAngularVelocity)
                {
                    var ratio = _maxAngularVelocity / MathF.Abs(rotation);
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

            for (var i = 0; i < _positionIterations; i++)
            {
                var contactsOkay = _contactSolver.SolvePositionConstraints();
                var jointsOkay = true;

                for (int j = 0; j < JointCount; ++j)
                {
                    Joint joint = _joints[j];

                    if (!joint.Enabled)
                        continue;

                    bool jointOkay = joint.SolvePositionConstraints(SolverData);

                    jointsOkay = jointsOkay && jointOkay;
                }

                if (contactsOkay && jointsOkay)
                {
                    _positionSolved = true;
                    break;
                }
            }
        }

        private void ReadBodies(int start, int end, float frameTime, Vector2 gravity)
        {
            for (var i = start; i < end; i++)
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
                        linearVelocity += (gravity + body.Force * body.InvMass) * frameTime;

                    angularVelocity += frameTime * body.InvI * body.Torque;

                    linearVelocity *= Math.Clamp(1.0f - frameTime * body.LinearDamping, 0.0f, 1.0f);
                    angularVelocity *= Math.Clamp(1.0f - frameTime * body.AngularDamping, 0.0f, 1.0f);
                }

                _positions[i] = position;
                _angles[i] = angle;
                _linearVelocities[i] = linearVelocity;
                _angularVelocities[i] = angularVelocity;
            }
        }

        internal void UpdateBodies(List<(ITransformComponent, IPhysBody)> deferredUpdates)
        {
            // Update data on bodies by copying the buffers back
            for (var i = 0; i < BodyCount; i++)
            {
                var body = Bodies[i];

                // So technically we don't /need/ to skip static bodies here but it saves us having to check for deferred updates so we'll do it anyway.
                // Plus calcing worldpos can be costly so we skip that too which is nice.
                if (body.BodyType == BodyType.Static) continue;

                /*
                 * Handle new position
                 */
                var bodyPos = _positions[i];
                var angle = _angles[i];

                // Temporary NaN guards until PVS is fixed.
                if (!float.IsNaN(bodyPos.X) && !float.IsNaN(bodyPos.Y))
                {
                    // body.Sweep.Center = bodyPos;
                    // body.Sweep.Angle = angle;

                    // DebugTools.Assert(!float.IsNaN(bodyPos.X) && !float.IsNaN(bodyPos.Y));
                    var transform = body.Owner.Transform;

                    // Defer MoveEvent / RotateEvent until the end of the physics step so cache can be better.
                    transform.DeferUpdates = true;
                    transform.WorldPosition = bodyPos;
                    transform.WorldRotation = angle;
                    transform.DeferUpdates = false;

                    if (transform.UpdatesDeferred)
                    {
                        deferredUpdates.Add((transform, body));
                    }
                }

                var linVelocity = _linearVelocities[i];

                if (!float.IsNaN(linVelocity.X) && !float.IsNaN(linVelocity.Y))
                {
                    body.LinearVelocity = linVelocity;
                }

                if (!float.IsNaN(_angularVelocities[i]))
                {
                    body.AngularVelocity = _angularVelocities[i];
                }
            }
        }

        internal void SleepBodies(bool prediction, float frameTime)
        {
            if (LoneIsland)
            {
                if (!prediction && _sleepAllowed)
                {
                    for (var i = 0; i < BodyCount; i++)
                    {
                        var body = Bodies[i];

                        if (body.BodyType == BodyType.Static) continue;

                        if (!body.SleepingAllowed ||
                            body.AngularVelocity * body.AngularVelocity > _angTolSqr ||
                            Vector2.Dot(body.LinearVelocity, body.LinearVelocity) > _linTolSqr)
                        {
                            body.SleepTime = 0.0f;
                        }
                        else
                        {
                            body.SleepTime += frameTime;
                        }

                        if (body.SleepTime >= _timeToSleep && _positionSolved)
                        {
                            body.Awake = false;
                        }
                    }
                }
            }
            else
            {
                // Sleep bodies if needed. Prediction won't accumulate sleep-time for bodies.
                if (!prediction && _sleepAllowed)
                {
                    var minSleepTime = float.MaxValue;

                    for (var i = 0; i < BodyCount; i++)
                    {
                        var body = Bodies[i];

                        if (body.BodyType == BodyType.Static) continue;

                        if (!body.SleepingAllowed ||
                            body.AngularVelocity * body.AngularVelocity > _angTolSqr ||
                            Vector2.Dot(body.LinearVelocity, body.LinearVelocity) > _linTolSqr)
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

                    if (minSleepTime >= _timeToSleep && _positionSolved)
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
