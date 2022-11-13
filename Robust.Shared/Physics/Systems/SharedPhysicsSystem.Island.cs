using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Dynamics.Contacts;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Systems;

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

public abstract partial class SharedPhysicsSystem
{
    /*
     * Handles island generation and overall solve code.
     */
    private const int MaxIslands = 256;

    private readonly ObjectPool<List<PhysicsComponent>> _islandBodyPool =
        new DefaultObjectPool<List<PhysicsComponent>>(new ListPolicy<PhysicsComponent>(), MaxIslands);

    private readonly ObjectPool<List<Contact>> _islandContactPool =
        new DefaultObjectPool<List<Contact>>(new ListPolicy<Contact>(), MaxIslands);

    private readonly ObjectPool<List<Joint>> _islandJointPool = new DefaultObjectPool<List<Joint>>(new ListPolicy<Joint>(), MaxIslands);

    internal record struct IslandData(int Index, bool LoneIsland, List<PhysicsComponent> Bodies, List<Contact> Contacts, List<Joint> Joints)
    {
        public readonly int Index = Index;
        public readonly bool LoneIsland = LoneIsland;
        public readonly List<PhysicsComponent> Bodies = Bodies;
        public readonly List<Contact> Contacts = Contacts;
        public readonly List<Joint> Joints = Joints;
        public bool PositionSolved = false;
    }

    // Caching for island generation.
    private readonly HashSet<PhysicsComponent> _islandSet = new(64);
    private readonly Stack<PhysicsComponent> _bodyStack = new(64);
    private readonly List<PhysicsComponent> _awakeBodyList = new(256);

    // Config
    private bool _warmStarting;
    private float _maxLinearCorrection;
    private float _maxAngularCorrection;
    private int _velocityIterations;
    private int _positionIterations;

    private void InitializeIsland()
    {
        _configManager.OnValueChanged(CVars.WarmStarting, SetWarmStarting, true);
        _configManager.OnValueChanged(CVars.MaxLinearCorrection, SetMaxLinearCorrection, true);
        _configManager.OnValueChanged(CVars.MaxAngularCorrection, SetMaxAngularCorrection, true);
        _configManager.OnValueChanged(CVars.VelocityIterations, SetVelocityIterations, true);
        _configManager.OnValueChanged(CVars.PositionIterations, SetPositionIterations, true);
    }

    private void ShutdownIsland()
    {
        _configManager.UnsubValueChanged(CVars.WarmStarting, SetWarmStarting);
        _configManager.UnsubValueChanged(CVars.MaxLinearCorrection, SetMaxLinearCorrection);
        _configManager.UnsubValueChanged(CVars.MaxAngularCorrection, SetMaxAngularCorrection);
        _configManager.UnsubValueChanged(CVars.VelocityIterations, SetVelocityIterations);
        _configManager.UnsubValueChanged(CVars.PositionIterations, SetPositionIterations);
    }

    private void SetWarmStarting(bool value) => _warmStarting = value;
    private void SetMaxLinearCorrection(float value) => _maxLinearCorrection = value;
    private void SetMaxAngularCorrection(float value) => _maxAngularCorrection = value;
    private void SetVelocityIterations(int value) => _velocityIterations = value;
    private void SetPositionIterations(int value) => _positionIterations = value;

    /// <summary>
    ///     Where the magic happens.
    /// </summary>
    public void Step(SharedPhysicsMapComponent component, float frameTime, bool prediction)
    {
        // Box2D does this at the end of a step and also here when there's a fixture update.
        // Given external stuff can move bodies we'll just do this here.
        // Unfortunately this NEEDS to be predicted to make pushing remotely fucking good.
        _broadphase.FindNewContacts(component, component.MapId);

        var invDt = frameTime > 0.0f ? 1.0f / frameTime : 0.0f;
        var dtRatio = component._invDt0 * frameTime;

        var updateBeforeSolve = new PhysicsUpdateBeforeMapSolveEvent(prediction, component, frameTime);
        RaiseLocalEvent(ref updateBeforeSolve);

        component.ContactManager.Collide();
        // Don't run collision behaviors during FrameUpdate?
        if (!prediction)
            component.ContactManager.PreSolve(frameTime);

        // Integrate velocities, solve velocity constraints, and do integration.
        Solve(component, frameTime, dtRatio, invDt, prediction);

        // TODO: SolveTOI

        var updateAfterSolve = new PhysicsUpdateAfterMapSolveEvent(prediction, component, frameTime);
        RaiseLocalEvent(ref updateAfterSolve);

        // Box2d recommends clearing (if you are) during fixed updates rather than variable if you are using it
        if (!prediction && component.AutoClearForces)
            ClearForces(component);

        component._invDt0 = invDt;
    }

    private void ClearForces(SharedPhysicsMapComponent component)
    {
        foreach (var body in component.AwakeBodies)
        {
            // TODO: Netsync
            body.Force = Vector2.Zero;
            body.Torque = 0.0f;
        }
    }

    private void Solve(SharedPhysicsMapComponent component, float frameTime, float dtRatio, float invDt, bool prediction)
    {
        var contactNode = component.ContactManager._activeContacts.First;

        while (contactNode != null)
        {
            var contact = contactNode.Value;
            contactNode = contactNode.Next;
            contact.Flags &= ~ContactFlags.Island;
        }

        // Build and simulated islands from awake bodies.
        _bodyStack.EnsureCapacity(component.AwakeBodies.Count);
        _islandSet.EnsureCapacity(component.AwakeBodies.Count);
        _awakeBodyList.AddRange(component.AwakeBodies);

        var bodyQuery = GetEntityQuery<PhysicsComponent>();
        var metaQuery = GetEntityQuery<MetaDataComponent>();
        var jointQuery = GetEntityQuery<JointComponent>();
        var islandIndex = 0;
        var loneIsland = new IslandData(islandIndex++, true, _islandBodyPool.Get(), _islandContactPool.Get(), _islandJointPool.Get());
        var islands = new List<IslandData>();

        // Build the relevant islands / graphs for all bodies.
        foreach (var seed in _awakeBodyList)
        {
            // I tried not running prediction for non-contacted entities but unfortunately it looked like shit
            // when contact broke so if you want to try that then GOOD LUCK.
            if (seed.Island) continue;

            if (!metaQuery.TryGetComponent(seed.Owner, out var metadata))
            {
                _sawmill.Error($"Found deleted entity {ToPrettyString(seed.Owner)} on map!");
                component.RemoveSleepBody(seed);
                continue;
            }

            if ((metadata.EntityPaused && !seed.IgnorePaused) ||
                (prediction && !seed.Predict) ||
                !seed.CanCollide ||
                seed.BodyType == BodyType.Static)
            {
                continue;
            }

            // Start of a new island
            var bodies = _islandBodyPool.Get();
            var contacts = _islandContactPool.Get();
            var joints = _islandJointPool.Get();
            _bodyStack.Push(seed);

            seed.Island = true;

            while (_bodyStack.TryPop(out var body))
            {
                bodies.Add(body);
                _islandSet.Add(body);

                // Static bodies don't propagate islands
                if (body.BodyType == BodyType.Static) continue;

                // As static bodies can never be awake (unlike Farseer) we'll set this after the check.
                SetAwake(body, true, updateSleepTime: false);

                var node = body.Contacts.First;

                while (node != null)
                {
                    var contact = node.Value;
                    node = node.Next;

                    // Has this contact already been added to an island?
                    if ((contact.Flags & ContactFlags.Island) != 0x0) continue;

                    // Is this contact solid and touching?
                    if (!contact.Enabled || !contact.IsTouching) continue;

                    // Skip sensors.
                    if (contact.FixtureA?.Hard != true || contact.FixtureB?.Hard != true) continue;

                    contacts.Add(contact);
                    contact.Flags |= ContactFlags.Island;
                    var bodyA = contact.FixtureA!.Body;
                    var bodyB = contact.FixtureB!.Body;

                    var other = bodyA == body ? bodyB : bodyA;

                    // Was the other body already added to this island?
                    if (other.Island) continue;

                    _bodyStack.Push(other);
                    other.Island = true;
                }

                if (!jointQuery.TryGetComponent(body.Owner, out var jointComponent)) continue;

                foreach (var (_, joint) in jointComponent.Joints)
                {
                    if (joint.IslandFlag) continue;

                    var other = joint.BodyAUid == body.Owner
                        ? bodyQuery.GetComponent(joint.BodyBUid)
                        : bodyQuery.GetComponent(joint.BodyAUid);

                    // Don't simulate joints connected to inactive bodies.
                    if (!other.CanCollide) continue;

                    joints.Add(joint);
                    joint.IslandFlag = true;

                    if (other.Island) continue;

                    _bodyStack.Push(other);
                    other.Island = true;
                }
            }

            int idx;

            // Bodies not touching anything, hence we can just add it to the lone island.
            if (contacts.Count == 0 && joints.Count == 0)
            {
                DebugTools.Assert(bodies.Count == 1 && bodies[0].BodyType != BodyType.Static);
                loneIsland.Bodies.Add(bodies[0]);
                idx = loneIsland.Index;
            }
            else
            {
                var data = new IslandData(islandIndex++, false, bodies, contacts, joints);
                islands.Add(data);
                idx = data.Index;
            }

            // Allow static bodies to be re-used in other islands
            for (var i = 0; i < bodies.Count; i++)
            {
                var body = bodies[i];

                // Static bodies can participate in other islands
                if (body.BodyType == BodyType.Static)
                {
                    body.Island = false;
                }

                body.IslandIndex[idx] = i;
            }
        }

        // If we didn't use lone island just return it.
        if (loneIsland.Bodies.Count > 0)
        {
            islands.Add(loneIsland);
        }
        else
        {
            ReturnIsland(loneIsland);
        }

        SolveIslands(component, islands, frameTime, dtRatio, invDt, prediction);

        foreach (var island in islands)
        {
            ReturnIsland(island);
        }

        Cleanup(frameTime);
    }

    private void ReturnIsland(IslandData island)
    {
        _islandBodyPool.Return(island.Bodies);
        _islandContactPool.Return(island.Contacts);

        foreach (var joint in island.Joints)
        {
            joint.IslandFlag = false;
        }

        _islandJointPool.Return(island.Joints);
    }

    protected virtual void Cleanup(float frameTime)
    {
        foreach (var body in _islandSet)
        {
            if (!body.Island || body.Deleted)
            {
                continue;
            }

            body.IslandIndex.Clear();
            body.Island = false;
            DebugTools.Assert(body.BodyType != BodyType.Static);

            // So Box2D would update broadphase here buutttt we'll just wait until MoveEvent queue is used.
        }

        _islandSet.Clear();
        _awakeBodyList.Clear();
    }

    private void SolveIslands(SharedPhysicsMapComponent component, List<IslandData> islands, float frameTime, float dtRatio, float invDt, bool prediction)
    {
        // Islands are already pre-sorted
        var iBegin = 0;
        var gravity = component.Gravity;

        var data = new SolverData(frameTime, dtRatio, invDt, _warmStarting, _maxLinearCorrection, _maxAngularCorrection, _velocityIterations, _positionIterations);

        islands.Sort((x, y) => y.Contacts.Count.CompareTo(x.Contacts.Count) + y.Joints.Count.CompareTo(x.Joints.Count));

#if DEBUG
        foreach (var island in islands)
        {
            RaiseLocalEvent(new IslandSolveMessage(island.Bodies));
        }
#endif

        while (iBegin < islands.Count)
        {
            var island = islands[iBegin];

            if (!InternalParallel(island))
                break;

            SolveIsland(ref island, data, gravity, frameTime, dtRatio, invDt, prediction);
            iBegin++;
            // TODO: Submit rest in parallel if applicable
        }

        Parallel.For(iBegin, islands.Count, i =>
        {
            var island = islands[i];
            SolveIsland(ref island, data, gravity, frameTime, dtRatio, invDt, prediction);
        });

        // Update bodies sequentially to avoid race conditions. May be able to do this parallel someday
        // but easier to just do this for now.
        foreach (var island in islands)
        {
            UpdateBodies(island, component._deferredUpdates);
            SleepBodies(island, prediction, frameTime);
        }
    }

    /// <summary>
    /// Can we run the island in parallel internally, otherwise solve it in parallel with the rest.
    /// </summary>
    /// <param name="island"></param>
    /// <returns></returns>
    private bool InternalParallel(IslandData island)
    {
        return island.Contacts.Count > 32 || island.Joints.Count > 32;
    }

    /// <summary>
    ///     Go through all the bodies in this island and solve.
    /// </summary>
    private void SolveIsland(ref IslandData island, SolverData data, Vector2 gravity, float frameTime, float dtRatio, float invDt, bool prediction)
    {
        var bodyCount = island.Bodies.Count;
        var positions = ArrayPool<Vector2>.Shared.Rent(bodyCount);
        var angles = ArrayPool<float>.Shared.Rent(bodyCount);
        var linearVelocities = ArrayPool<Vector2>.Shared.Rent(bodyCount);
        var angularVelocities = ArrayPool<float>.Shared.Rent(bodyCount);

        for (var i = 0; i < island.Bodies.Count; i++)
        {
            var body = island.Bodies[i];

            // Didn't use the old variable names because they're hard to read
            var transform = _physicsManager.EnsureTransform(body);
            var position = Physics.Transform.Mul(transform, body.LocalCenter);
            // DebugTools.Assert(!float.IsNaN(position.X) && !float.IsNaN(position.Y));
            var angle = transform.Quaternion2D.Angle;

            // var bodyTransform = body.GetTransform();
            // DebugTools.Assert(bodyTransform.Position.EqualsApprox(position) && MathHelper.CloseTo(angle, bodyTransform.Quaternion2D.Angle));

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

            positions[i] = position;
            angles[i] = angle;
            linearVelocities[i] = linearVelocity;
            angularVelocities[i] = angularVelocity;
        }

        var solver = new ContactSolver();

        // Pass the data into the solver
        solver.Reset(data, island, linearVelocities, angularVelocities, positions, angles);

        solver.InitializeVelocityConstraints();

        if (data.WarmStarting)
        {
            solver.WarmStart();
        }

        var jointCount = island.Joints.Count;

        if (jointCount > 0)
        {
            var bodyQuery = GetEntityQuery<PhysicsComponent>();

            for (var i = 0; i < island.Joints.Count; i++)
            {
                var joint = island.Joints[i];
                if (!joint.Enabled) continue;

                var bodyA = bodyQuery.GetComponent(joint.BodyAUid);
                var bodyB = bodyQuery.GetComponent(joint.BodyBUid);
                joint.InitVelocityConstraints(data, bodyA, bodyB);
            }
        }

        // Velocity solver
        for (var i = 0; i < data.VelocityIterations; i++)
        {
            for (var j = 0; j < jointCount; ++j)
            {
                var joint = island.Joints[j];

                if (!joint.Enabled)
                    continue;

                joint.SolveVelocityConstraints(data);

                var error = joint.Validate(invDt);

                if (error > 0.0f)
                    island.BrokenJoints.Add((joint, error));
            }

            solver.SolveVelocityConstraints();
        }

        // Store for warm starting.
        solver.StoreImpulses();

        // Integrate positions
        for (var i = 0; i < bodyCount; i++)
        {
            var linearVelocity = linearVelocities[i];
            var angularVelocity = angularVelocities[i];

            var position = positions[i];
            var angle = angles[i];

            var translation = linearVelocity * frameTime;
            if (translation.Length > data.MaxLinearVelocity)
            {
                var ratio = data.MaxLinearVelocity / translation.Length;
                linearVelocity *= ratio;
            }

            var rotation = angularVelocity * frameTime;
            if (rotation * rotation > data.MaxAngularVelocity)
            {
                var ratio = data.MaxAngularVelocity / MathF.Abs(rotation);
                angularVelocity *= ratio;
            }

            // Integrate
            position += linearVelocity * frameTime;
            angle += angularVelocity * frameTime;

            linearVelocities[i] = linearVelocity;
            angularVelocities[i] = angularVelocity;
            positions[i] = position;
            angles[i] = angle;
        }

        island.PositionSolved = false;

        for (var i = 0; i < data.PositionIterations; i++)
        {
            var contactsOkay = solver.SolvePositionConstraints();
            var jointsOkay = true;

            for (int j = 0; j < island.Joints.Count; ++j)
            {
                var joint = island.Joints[j];

                if (!joint.Enabled)
                    continue;

                var jointOkay = joint.SolvePositionConstraints(data);

                jointsOkay = jointsOkay && jointOkay;
            }

            if (contactsOkay && jointsOkay)
            {
                island.PositionSolved = true;
                break;
            }
        }
    }

    internal void UpdateBodies(IslandData island, HashSet<TransformComponent> deferredUpdates, Vector2[] positions, float[] angles, Vector2[] linearVelocities, float[] angularVelocities)
    {
        foreach (var (joint, error) in island.BrokenJoints)
        {
            var msg = new Joint.JointBreakMessage(joint, MathF.Sqrt(error));
            RaiseLocalEvent(joint.BodyAUid, msg, false);
            RaiseLocalEvent(joint.BodyBUid, msg, false);
            RaiseLocalEvent(EventSource.Local, msg);
        }

        var xforms = GetEntityQuery<TransformComponent>();

        // Update data on bodies by copying the buffers back
        for (var i = 0; i < island.Bodies.Count; i++)
        {
            var body = island.Bodies[i];

            // So technically we don't /need/ to skip static bodies here but it saves us having to check for deferred updates so we'll do it anyway.
            // Plus calcing worldpos can be costly so we skip that too which is nice.
            if (body.BodyType == BodyType.Static) continue;

            /*
             * Handle new position
             */
            var bodyPos = positions[i];
            var angle = angles[i];

            // Temporary NaN guards until PVS is fixed.
            if (!float.IsNaN(bodyPos.X) && !float.IsNaN(bodyPos.Y))
            {
                var q = new Quaternion2D(angle);

                bodyPos -= Physics.Transform.Mul(q, body.LocalCenter);
                var transform = xforms.GetComponent(body.Owner);

                // Defer MoveEvent until the end of the physics step so cache can be better.
                transform.DeferUpdates = true;
                _transform.SetWorldPositionRotation(transform, bodyPos, angle, xforms);
                transform.DeferUpdates = false;

                // Unfortunately we can't cache the position and angle here because if our parent's position
                // changes then this is immediately invalidated.
                if (transform.UpdatesDeferred)
                {
                    deferredUpdates.Add(transform);
                }
            }

            var linVelocity = linearVelocities[i];

            if (!float.IsNaN(linVelocity.X) && !float.IsNaN(linVelocity.Y))
            {
                SetLinearVelocity(body, linVelocity);
            }

            var angVelocity = _angularVelocities[i];

            if (!float.IsNaN(angVelocity))
            {
                SetAngularVelocity(body, angVelocity);
            }
        }
    }

    private void SleepBodies(IslandData island, SolverData data, bool prediction, float frameTime)
    {
        if (island.LoneIsland)
        {
            if (!prediction && data.SleepAllowed)
            {
                for (var i = 0; i < island.Bodies.Count; i++)
                {
                    var body = island.Bodies[i];

                    if (body.BodyType == BodyType.Static) continue;

                    if (!body.SleepingAllowed ||
                        body.AngularVelocity * body.AngularVelocity > data.AngToLSqr ||
                        Vector2.Dot(body.LinearVelocity, body.LinearVelocity) > data.LinTolSqr)
                    {
                        SetSleepTime(body, 0f);
                    }
                    else
                    {
                        SetSleepTime(body, body.SleepTime + frameTime);
                    }

                    if (body.SleepTime >= data.TimeToSleep && island.PositionSolved)
                    {
                        SetAwake(body, false);
                    }
                }
            }
        }
        else
        {
            // Sleep bodies if needed. Prediction won't accumulate sleep-time for bodies.
            if (!prediction && data.SleepAllowed)
            {
                var minSleepTime = float.MaxValue;

                for (var i = 0; i < island.Bodies.Count; i++)
                {
                    var body = island.Bodies[i];

                    if (body.BodyType == BodyType.Static) continue;

                    if (!body.SleepingAllowed ||
                        body.AngularVelocity * body.AngularVelocity > data.AngTolSqr ||
                        Vector2.Dot(body.LinearVelocity, body.LinearVelocity) > data.LinTolSqr)
                    {
                        SetSleepTime(body, 0f);
                        minSleepTime = 0.0f;
                    }
                    else
                    {
                        SetSleepTime(body, body.SleepTime + frameTime);
                        minSleepTime = MathF.Min(minSleepTime, body.SleepTime);
                    }
                }

                if (minSleepTime >= data.TimeToSleep && island.PositionSolved)
                {
                    for (var i = 0; i < island.Bodies.Count; i++)
                    {
                        var body = island.Bodies[i];
                        SetAwake(body, false);
                    }
                }
            }
        }
    }
}
