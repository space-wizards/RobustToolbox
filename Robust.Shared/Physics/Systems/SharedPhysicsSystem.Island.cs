using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using Robust.Shared.GameObjects;
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
     * Handles island generation and constraints solver code.
     */
    private const int MaxIslands = 256;

    private readonly ObjectPool<List<PhysicsComponent>> _islandBodyPool =
        new DefaultObjectPool<List<PhysicsComponent>>(new ListPolicy<PhysicsComponent>(), MaxIslands);

    private readonly ObjectPool<List<Contact>> _islandContactPool =
        new DefaultObjectPool<List<Contact>>(new ListPolicy<Contact>(), MaxIslands);

    /// <summary>
    /// Due to joint relays we need to track the dummy joint and the original joint.
    /// </summary>
    private readonly ObjectPool<List<(Joint Original, Joint Joint)>> _islandJointPool =
        new DefaultObjectPool<List<(Joint Original, Joint Joint)>>(new ListPolicy<(Joint Original, Joint Joint)>(), MaxIslands);

    internal record struct IslandData(
        int Index,
        bool LoneIsland,
        List<PhysicsComponent> Bodies,
        List<Contact> Contacts,
        List<(Joint Original, Joint Joint)> Joints,
        List<(Joint Joint, float Error)> BrokenJoints)
    {
        /// <summary>
        /// Island index to be used for bodies to identify which island they're in.
        /// </summary>
        public readonly int Index = Index;

        /// <summary>
        /// Are we the special island that has all contact-less bodies in it?
        /// This is treated separately for sleep purposes.
        /// </summary>
        public readonly bool LoneIsland = LoneIsland;

        /// <summary>
        /// Offset in the data arrays
        /// </summary>
        public int Offset = 0;

        public readonly List<PhysicsComponent> Bodies = Bodies;
        public readonly List<Contact> Contacts = Contacts;
        public readonly List<(Joint Original, Joint Joint)> Joints = Joints;
        public bool PositionSolved = false;
        public readonly List<(Joint Joint, float Error)> BrokenJoints = BrokenJoints;
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
    private float _maxLinearVelocity;
    private float _maxAngularVelocity;
    private float _maxTranslationPerTick;
    private float _maxRotationPerTick;
    private int _tickRate;
    private bool _sleepAllowed;
    protected float AngularToleranceSqr;
    protected float LinearToleranceSqr;
    protected float TimeToSleep;
    private float _velocityThreshold;
    private float _baumgarte;

    private const int VelocityConstraintsPerThread = 16;
    private const int PositionConstraintsPerThread = 16;

    #region Setup

    private void InitializeIsland()
    {
        Subs.CVar(_cfg, CVars.NetTickrate, SetTickRate, true);
        Subs.CVar(_cfg, CVars.WarmStarting, SetWarmStarting, true);
        Subs.CVar(_cfg, CVars.MaxLinearCorrection, SetMaxLinearCorrection, true);
        Subs.CVar(_cfg, CVars.MaxAngularCorrection, SetMaxAngularCorrection, true);
        Subs.CVar(_cfg, CVars.VelocityIterations, SetVelocityIterations, true);
        Subs.CVar(_cfg, CVars.PositionIterations, SetPositionIterations, true);
        Subs.CVar(_cfg, CVars.MaxLinVelocity, SetMaxLinearVelocity, true);
        Subs.CVar(_cfg, CVars.MaxAngVelocity, SetMaxAngularVelocity, true);
        Subs.CVar(_cfg, CVars.SleepAllowed, SetSleepAllowed, true);
        Subs.CVar(_cfg, CVars.AngularSleepTolerance, SetAngularToleranceSqr, true);
        Subs.CVar(_cfg, CVars.LinearSleepTolerance, SetLinearToleranceSqr, true);
        Subs.CVar(_cfg, CVars.TimeToSleep, SetTimeToSleep, true);
        Subs.CVar(_cfg, CVars.VelocityThreshold, SetVelocityThreshold, true);
        Subs.CVar(_cfg, CVars.Baumgarte, SetBaumgarte, true);
    }

    private void SetWarmStarting(bool value) => _warmStarting = value;
    private void SetMaxLinearCorrection(float value) => _maxLinearCorrection = value;
    private void SetMaxAngularCorrection(float value) => _maxAngularCorrection = value;
    private void SetVelocityIterations(int value) => _velocityIterations = value;
    private void SetPositionIterations(int value) => _positionIterations = value;
    private void SetMaxLinearVelocity(float value)
    {
        _maxLinearVelocity = value;
        UpdateMaxTranslation();
    }

    private void SetMaxAngularVelocity(float value)
    {
        _maxAngularVelocity = value;
        UpdateMaxRotation();
    }

    private void SetTickRate(int value)
    {
        _tickRate = value;
        UpdateMaxTranslation();
        UpdateMaxRotation();
    }

    private void SetSleepAllowed(bool value) => _sleepAllowed = value;
    private void SetAngularToleranceSqr(float value) => AngularToleranceSqr = value;
    private void SetLinearToleranceSqr(float value) => LinearToleranceSqr = value;
    private void SetTimeToSleep(float value) => TimeToSleep = value;
    private void SetVelocityThreshold(float value) => _velocityThreshold = value;
    private void SetBaumgarte(float value) => _baumgarte = value;

    private void UpdateMaxTranslation()
    {
        _maxTranslationPerTick = _maxLinearVelocity / _tickRate;
    }

    private void UpdateMaxRotation()
    {
        _maxRotationPerTick = (MathF.Tau * _maxAngularVelocity) / _tickRate;
    }

    #endregion

    /// <summary>
    ///     Where the magic happens.
    /// </summary>
    public void Step(EntityUid uid, PhysicsMapComponent component, float frameTime, bool prediction)
    {
        var invDt = frameTime > 0.0f ? 1.0f / frameTime : 0.0f;
        var dtRatio = component._invDt0 * frameTime;

        // Integrate velocities, solve velocity constraints, and do integration.
        Solve(uid, component, frameTime, dtRatio, invDt, prediction);

        // TODO: SolveTOI

        var updateAfterSolve = new PhysicsUpdateAfterMapSolveEvent(prediction, component, frameTime);
        RaiseLocalEvent(ref updateAfterSolve);

        // Box2d recommends clearing (if you are) during fixed updates rather than variable if you are using it
        if (!prediction && component.AutoClearForces)
            ClearForces(component);

        component._invDt0 = invDt;
    }

    private void ClearForces(PhysicsMapComponent component)
    {
        foreach (var body in component.AwakeBodies)
        {
            // TODO: Netsync
            body.Force = Vector2.Zero;
            body.Torque = 0.0f;
        }
    }

    private void Solve(EntityUid uid, PhysicsMapComponent component, float frameTime, float dtRatio, float invDt, bool prediction)
    {
        // Build and simulated islands from awake bodies.
        _bodyStack.EnsureCapacity(component.AwakeBodies.Count);
        _islandSet.EnsureCapacity(component.AwakeBodies.Count);
        _awakeBodyList.AddRange(component.AwakeBodies);

        var bodyQuery = GetEntityQuery<PhysicsComponent>();
        var metaQuery = GetEntityQuery<MetaDataComponent>();
        var jointQuery = GetEntityQuery<JointComponent>();
        var jointRelayQuery = GetEntityQuery<JointRelayTargetComponent>();

        var islandIndex = 0;
        var loneIsland = new IslandData(
            islandIndex++,
            true,
            _islandBodyPool.Get(),
            _islandContactPool.Get(),
            _islandJointPool.Get(),
            new List<(Joint Joint, float Error)>());

        var islands = new List<IslandData>();
        var islandJoints = new List<(Joint Original, Joint Joint)>();

        // Build the relevant islands / graphs for all bodies.
        foreach (var seed in _awakeBodyList)
        {
            // I tried not running prediction for non-contacted entities but unfortunately it looked like shit
            // when contact broke so if you want to try that then GOOD LUCK.
            if (seed.Island) continue;

            var seedUid = seed.Owner;

            if (!metaQuery.TryGetComponent(seedUid, out var metadata))
            {
                Log.Error($"Found deleted entity {ToPrettyString(seedUid)} on map!");
                RemoveSleepBody(seedUid, seed, component);
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
                var bodyUid = body.Owner;

                bodies.Add(body);
                _islandSet.Add(body);

                // Static bodies don't propagate islands
                if (body.BodyType == BodyType.Static) continue;

                // As static bodies can never be awake (unlike Farseer) we'll set this after the check.
                SetAwake(bodyUid, body, true, updateSleepTime: false);

                var node = body.Contacts.First;

                while (node != null)
                {
                    var contact = node.Value;
                    node = node.Next;

                    // Has this contact already been added to an island / is it pre-init?
                    if ((contact.Flags & (ContactFlags.Island | ContactFlags.PreInit)) != 0x0) continue;

                    // Is this contact solid and touching?
                    if (!contact.Enabled || !contact.IsTouching) continue;

                    // Skip sensors.
                    if (contact.FixtureA?.Hard != true || contact.FixtureB?.Hard != true) continue;

                    contacts.Add(contact);
                    contact.Flags |= ContactFlags.Island;
                    var bodyA = contact.BodyA!;
                    var bodyB = contact.BodyB!;

                    var other = bodyA == body ? bodyB : bodyA;

                    // Was the other body already added to this island?
                    if (other.Island) continue;

                    _bodyStack.Push(other);
                    other.Island = true;
                }

                // Handle joints
                if (jointRelayQuery.TryGetComponent(bodyUid, out var relayComp))
                {
                    foreach (var relay in relayComp.Relayed)
                    {
                        if (!jointQuery.TryGetComponent(relay, out var jointComp))
                            continue;

                        foreach (var joint in jointComp.GetJoints.Values)
                        {
                            if (joint.IslandFlag)
                                continue;

                            var uidA = joint.BodyAUid;
                            var uidB = joint.BodyBUid;
                            DebugTools.AssertNotEqual(uidA, uidB);

                            if (jointQuery.TryGetComponent(uidA, out var jointCompA) &&
                                jointCompA.Relay != null)
                            {
                                DebugTools.AssertNotEqual(uidB, jointCompA.Relay.Value);
                                uidA = jointCompA.Relay.Value;
                            }

                            if (jointQuery.TryGetComponent(uidB, out var jointCompB) &&
                                jointCompB.Relay != null)
                            {
                                DebugTools.AssertNotEqual(uidA, jointCompB.Relay.Value);
                                uidB = jointCompB.Relay.Value;
                            }

                            var copy = joint.Clone(uidA, uidB);
                            islandJoints.Add((joint, copy));
                            joint.IslandFlag = true;
                        }
                    }
                }

                if (jointQuery.TryGetComponent(bodyUid, out var jointComponent) &&
                    jointComponent.Relay == null)
                {
                    foreach (var joint in jointComponent.Joints.Values)
                    {
                        if (joint.IslandFlag)
                            continue;

                        var uidA = joint.BodyAUid;
                        var uidB = joint.BodyBUid;

                        if (jointQuery.TryGetComponent(uidA, out var jointCompA) &&
                            jointCompA.Relay != null)
                        {
                            uidA = jointCompA.Relay.Value;
                        }

                        if (jointQuery.TryGetComponent(uidB, out var jointCompB) &&
                            jointCompB.Relay != null)
                        {
                            uidB = jointCompB.Relay.Value;
                        }

                        var copy = joint.Clone(uidA, uidB);
                        islandJoints.Add((joint, copy));
                        joint.IslandFlag = true;
                    }
                }

                foreach (var (original, joint) in islandJoints)
                {
                    var bodyA = bodyQuery.GetComponent(joint.BodyAUid);
                    var bodyB = bodyQuery.GetComponent(joint.BodyBUid);

                    if (!bodyA.CanCollide || !bodyB.CanCollide)
                        continue;

                    joints.Add((original, joint));

                    if (!bodyA.Island)
                    {
                        _bodyStack.Push(bodyA);
                        bodyA.Island = true;
                    }

                    if (!bodyB.Island)
                    {
                        _bodyStack.Push(bodyB);
                        bodyB.Island = true;
                    }
                }

                islandJoints.Clear();
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
                var data = new IslandData(islandIndex++, false, bodies, contacts, joints, new List<(Joint Joint, float Error)>());
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

        SolveIslands(uid, component, islands, frameTime, dtRatio, invDt, prediction);

        foreach (var island in islands)
        {
            ReturnIsland(island);
        }

        Cleanup(component, frameTime);
    }

    private void ReturnIsland(IslandData island)
    {
        foreach (var body in island.Bodies)
        {
            DebugTools.Assert(body.IslandIndex.ContainsKey(island.Index));
            body.IslandIndex.Remove(island.Index);
        }

        _islandBodyPool.Return(island.Bodies);
        _islandContactPool.Return(island.Contacts);

        foreach (var (original, joint) in island.Joints)
        {
            // Do we need to copy data back to the original?
            if (original != joint)
            {
                joint.CopyTo(original);
            }

            original.IslandFlag = false;
        }

        _islandJointPool.Return(island.Joints);
        island.BrokenJoints.Clear();
    }

    protected virtual void Cleanup(PhysicsMapComponent component, float frameTime)
    {
        foreach (var body in _islandSet)
        {
            if (!body.Island || body.Deleted)
            {
                continue;
            }

            body.Island = false;
            DebugTools.Assert(body.BodyType != BodyType.Static);

            // So Box2D would update broadphase here buutttt we'll just wait until MoveEvent queue is used.
        }

        _islandSet.Clear();
        _awakeBodyList.Clear();
    }

    private void SolveIslands(EntityUid uid, PhysicsMapComponent component, List<IslandData> islands, float frameTime, float dtRatio, float invDt, bool prediction)
    {
        var iBegin = 0;
        var gravity = _gravity.GetGravity(uid);

        var data = new SolverData(
            frameTime,
            dtRatio,
            invDt,
            _warmStarting,
            _maxLinearCorrection,
            _maxAngularCorrection,
            _velocityIterations,
            _positionIterations,
            _maxLinearVelocity,
            _maxAngularVelocity,
            _maxTranslationPerTick,
            _maxRotationPerTick,
            _sleepAllowed,
            AngularToleranceSqr,
            LinearToleranceSqr,
            TimeToSleep,
            _velocityThreshold,
            _baumgarte
        );

        // We'll sort islands from internally parallel (due to lots of contacts) to running all the islands in parallel
        islands.Sort((x, y) => InternalParallel(y).CompareTo(InternalParallel(x)));

        var totalBodies = 0;
        var actualIslands = islands.ToArray();
        var xformQuery = GetEntityQuery<TransformComponent>();

        for (var i = 0; i < islands.Count; i++)
        {
            ref var island = ref actualIslands[i];
            island.Offset = totalBodies;
            UpdateLerpData(component, island.Bodies, xformQuery);

#if DEBUG
            RaiseLocalEvent(new IslandSolveMessage(island.Bodies));
#endif

            totalBodies += island.Bodies.Count;
        }

        // Actual solver here; cache the data for later.
        var solvedPositions = ArrayPool<Vector2>.Shared.Rent(totalBodies);
        var solvedAngles = ArrayPool<float>.Shared.Rent(totalBodies);
        var linearVelocities = ArrayPool<Vector2>.Shared.Rent(totalBodies);
        var angularVelocities = ArrayPool<float>.Shared.Rent(totalBodies);
        var sleepStatus = ArrayPool<bool>.Shared.Rent(totalBodies);
        // Cleanup any potentially stale data first.
        for (var i = 0; i < totalBodies; i++)
        {
            sleepStatus[i] = false;
        }

        var options = new ParallelOptions()
        {
            MaxDegreeOfParallelism = _parallel.ParallelProcessCount,
        };

        while (iBegin < actualIslands.Length)
        {
            ref var island = ref actualIslands[iBegin];

            if (!InternalParallel(island))
                break;

            SolveIsland(ref island, in data, options, gravity, prediction, solvedPositions, solvedAngles, linearVelocities, angularVelocities, sleepStatus);
            iBegin++;
        }

        Parallel.For(iBegin, actualIslands.Length, options, i =>
        {
            ref var island = ref actualIslands[i];
            SolveIsland(ref island, in data, null, gravity, prediction, solvedPositions, solvedAngles, linearVelocities, angularVelocities, sleepStatus);
        });

        // Update data sequentially
        for (var i = 0; i < actualIslands.Length; i++)
        {
            var island = actualIslands[i];

            UpdateBodies(in island, solvedPositions, solvedAngles, linearVelocities, angularVelocities, xformQuery);
            SleepBodies(in island, sleepStatus);
        }

        // Cleanup
        ArrayPool<Vector2>.Shared.Return(solvedPositions);
        ArrayPool<float>.Shared.Return(solvedAngles);
        ArrayPool<Vector2>.Shared.Return(linearVelocities);
        ArrayPool<float>.Shared.Return(angularVelocities);
        ArrayPool<bool>.Shared.Return(sleepStatus);
    }

    /// <summary>
    /// If this is the first time a body has been updated this tick update its position for lerping.
    /// Due to substepping we have to check it every time.
    /// </summary>
    protected virtual void UpdateLerpData(PhysicsMapComponent component, List<PhysicsComponent> bodies, EntityQuery<TransformComponent> xformQuery)
    {

    }

    /// <summary>
    /// Can we run the island in parallel internally, otherwise solve it in parallel with the rest.
    /// </summary>
    /// <param name="island"></param>
    /// <returns></returns>
    private static bool InternalParallel(IslandData island)
    {
        // Should lone island most times as well.
        return island.Bodies.Count > 128 || island.Contacts.Count > 128 || island.Joints.Count > 128;
    }

    /// <summary>
    ///     Go through all the bodies in this island and solve.
    /// </summary>
    private void SolveIsland(
        ref IslandData island,
        in SolverData data,
        ParallelOptions? options,
        Vector2 gravity,
        bool prediction,
        Vector2[] solvedPositions,
        float[] solvedAngles,
        Vector2[] linearVelocities,
        float[] angularVelocities,
        bool[] sleepStatus)
    {
        var bodyCount = island.Bodies.Count;
        var positions = ArrayPool<Vector2>.Shared.Rent(bodyCount);
        var angles = ArrayPool<float>.Shared.Rent(bodyCount);
        var offset = island.Offset;
        var xformQuery = GetEntityQuery<TransformComponent>();

        for (var i = 0; i < island.Bodies.Count; i++)
        {
            var body = island.Bodies[i];
            var (worldPos, worldRot) =
                _transform.GetWorldPositionRotation(xformQuery.GetComponent(body.Owner), xformQuery);

            var transform = new Transform(worldPos, worldRot);
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
                    linearVelocity += body.Force * data.FrameTime * body.InvMass;
                else
                    linearVelocity += (gravity + body.Force * body.InvMass) * data.FrameTime;

                angularVelocity += body.InvI * body.Torque * data.FrameTime;

                linearVelocity *= Math.Clamp(1.0f - data.FrameTime * body.LinearDamping, 0.0f, 1.0f);
                angularVelocity *= Math.Clamp(1.0f - data.FrameTime * body.AngularDamping, 0.0f, 1.0f);
            }

            positions[i] = position;
            angles[i] = angle;
            linearVelocities[i + offset] = linearVelocity;
            angularVelocities[i + offset] = angularVelocity;
        }

        var contactCount = island.Contacts.Count;
        var velocityConstraints = ArrayPool<ContactVelocityConstraint>.Shared.Rent(contactCount);
        var positionConstraints = ArrayPool<ContactPositionConstraint>.Shared.Rent(contactCount);

        // Pass the data into the solver
        ResetSolver(data, island, velocityConstraints, positionConstraints);

        InitializeVelocityConstraints(in data, in island, velocityConstraints, positionConstraints, positions, angles, linearVelocities, angularVelocities);

        if (data.WarmStarting)
        {
            WarmStart(data, island, velocityConstraints, linearVelocities, angularVelocities);
        }

        var jointCount = island.Joints.Count;
        var bodyQuery = GetEntityQuery<PhysicsComponent>();

        if (jointCount > 0)
        {
            for (var i = 0; i < island.Joints.Count; i++)
            {
                var joint = island.Joints[i].Joint;
                if (!joint.Enabled) continue;

                var bodyA = bodyQuery.GetComponent(joint.BodyAUid);
                var bodyB = bodyQuery.GetComponent(joint.BodyBUid);
                joint.InitVelocityConstraints(in data, in island, bodyA, bodyB, positions, angles, linearVelocities, angularVelocities);
            }
        }

        // Velocity solver
        for (var i = 0; i < data.VelocityIterations; i++)
        {
            for (var j = 0; j < jointCount; ++j)
            {
                var joint = island.Joints[j].Joint;

                if (!joint.Enabled)
                    continue;

                joint.SolveVelocityConstraints(in data, in island, linearVelocities, angularVelocities);

                var error = joint.Validate(data.InvDt);

                if (error > 0.0f)
                    island.BrokenJoints.Add((island.Joints[j].Original, error));
            }

            SolveVelocityConstraints(island, options, velocityConstraints, linearVelocities, angularVelocities);
        }

        // Store for warm starting.
        StoreImpulses(in island, velocityConstraints);

        var maxVel = data.MaxTranslation / data.FrameTime;
        var maxVelSq = maxVel * maxVel;
        var maxAngVel = data.MaxRotation / data.FrameTime;
        var maxAngVelSq = maxAngVel * maxAngVel;

        // Integrate positions
        for (var i = 0; i < bodyCount; i++)
        {
            var linearVelocity = linearVelocities[offset + i];
            var angularVelocity = angularVelocities[offset + i];

            var velSqr = linearVelocity.LengthSquared();
            if (velSqr > maxVelSq)
            {
                linearVelocity *= maxVel / MathF.Sqrt(velSqr);
                linearVelocities[offset + i] = linearVelocity;
            }

            if (angularVelocity * angularVelocity > maxAngVelSq)
            {
                angularVelocity *= maxAngVel / MathF.Abs(angularVelocity);
                angularVelocities[offset + i] = angularVelocity;
            }

            // Integrate
            positions[i] += linearVelocity * data.FrameTime;
            angles[i] += angularVelocity * data.FrameTime;
        }

        island.PositionSolved = false;

        for (var i = 0; i < data.PositionIterations; i++)
        {
            var contactsOkay = SolvePositionConstraints(data, in island, options, positionConstraints, positions, angles);
            var jointsOkay = true;

            for (var j = 0; j < island.Joints.Count; ++j)
            {
                var joint = island.Joints[j].Joint;

                if (!joint.Enabled)
                    continue;

                var jointOkay = joint.SolvePositionConstraints(in data, positions, angles);

                jointsOkay = jointsOkay && jointOkay;
            }

            if (contactsOkay && jointsOkay)
            {
                island.PositionSolved = true;
                break;
            }
        }

        // Transform the solved positions back into local terms
        // This means we can run the entire solver in parallel and not have to worry about stale world positions later
        // E.g. if a parent had its position updated then our worldposition is invalid
        // We can safely do this in parallel.

        // Solve positions now and store for later; we can't write this safely in parallel.
        var bodies = island.Bodies;

        if (options != null)
        {
            const int FinaliseBodies = 32;
            var batches = (int)MathF.Ceiling((float) bodyCount / FinaliseBodies);

            Parallel.For(0, batches, options, i =>
            {
                var start = i * FinaliseBodies;
                var end = Math.Min(bodyCount, start + FinaliseBodies);

                FinalisePositions(start, end, offset, bodies, xformQuery, positions, angles, solvedPositions, solvedAngles);
            });
        }
        else
        {
            FinalisePositions(0, bodyCount, offset, bodies,xformQuery, positions, angles, solvedPositions, solvedAngles);
        }

        // Check sleep status for all of the bodies
        // Writing sleep timer is safe but updating awake or not is not safe.

        // We have a special island for no-contact no-joint bodies and just run this custom sleeping behaviour
        // for it while still keeping the benefits of a big island.
        if (island.LoneIsland)
        {
            if (!prediction && data.SleepAllowed)
            {
                for (var i = 0; i < bodyCount; i++)
                {
                    var body = island.Bodies[i];

                    if (body.BodyType == BodyType.Static) continue;

                    if (!body.SleepingAllowed ||
                        body.AngularVelocity * body.AngularVelocity > data.AngTolSqr ||
                        Vector2.Dot(body.LinearVelocity, body.LinearVelocity) > data.LinTolSqr)
                    {
                        SetSleepTime(body, 0f);
                    }
                    else
                    {
                        SetSleepTime(body, body.SleepTime + data.FrameTime);
                    }

                    if (body.SleepTime >= data.TimeToSleep && island.PositionSolved)
                    {
                        sleepStatus[offset + i] = true;
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

                for (var i = 0; i < bodyCount; i++)
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
                        SetSleepTime(body, body.SleepTime + data.FrameTime);
                        minSleepTime = MathF.Min(minSleepTime, body.SleepTime);
                    }
                }

                if (minSleepTime >= data.TimeToSleep && island.PositionSolved)
                {
                    for (var i = 0; i < island.Bodies.Count; i++)
                    {
                        sleepStatus[offset + i] = true;
                    }
                }
            }
        }

        // Cleanup
        ArrayPool<Vector2>.Shared.Return(positions);
        ArrayPool<float>.Shared.Return(angles);
        ArrayPool<ContactVelocityConstraint>.Shared.Return(velocityConstraints);
        ArrayPool<ContactPositionConstraint>.Shared.Return(positionConstraints);
    }

    private void FinalisePositions(int start, int end, int offset, List<PhysicsComponent> bodies, EntityQuery<TransformComponent> xformQuery, Vector2[] positions, float[] angles, Vector2[] solvedPositions, float[] solvedAngles)
    {
        for (var i = start; i < end; i++)
        {
            var body = bodies[i];

            if (body.BodyType == BodyType.Static)
                continue;

            var xform = xformQuery.GetComponent(body.Owner);
            var parentXform = xformQuery.GetComponent(xform.ParentUid);
            var (_, parentRot, parentInvMatrix) = parentXform.GetWorldPositionRotationInvMatrix(xformQuery);
            var worldRot = (float) (parentRot + xform._localRotation);

            var angle = angles[i];

            var q = new Quaternion2D(angle);
            var adjustedPosition = positions[i] - Physics.Transform.Mul(q, body.LocalCenter);

            var solvedPosition = Vector2.Transform(adjustedPosition, parentInvMatrix);
            solvedPositions[offset + i] = solvedPosition - xform.LocalPosition;
            solvedAngles[offset + i] = angle - worldRot;
        }
    }

    /// <summary>
    /// Updates the positions, rotations, and velocities of all of the solved bodies.
    /// Run sequentially to avoid threading issues.
    /// </summary>
    private void UpdateBodies(
        in IslandData island,
        Vector2[] positions,
        float[] angles,
        Vector2[] linearVelocities,
        float[] angularVelocities,
        EntityQuery<TransformComponent> xformQuery)
    {
        foreach (var (joint, error) in island.BrokenJoints)
        {
            var ev = new JointBreakEvent(joint, MathF.Sqrt(error));
            RaiseLocalEvent(joint.BodyAUid, ref ev);
            RaiseLocalEvent(joint.BodyBUid, ref ev);
            RaiseLocalEvent(ref ev);
            joint.Dirty();
        }

        var offset = island.Offset;

        for (var i = 0; i < island.Bodies.Count; i++)
        {
            var body = island.Bodies[i];

            // So technically we don't /need/ to skip static bodies here but it saves us having to check for deferred updates so we'll do it anyway.
            // Plus calcing worldpos can be costly so we skip that too which is nice.
            if (body.BodyType == BodyType.Static) continue;

            var uid = body.Owner;
            var position = positions[offset + i];
            var angle = angles[offset + i];
            var xform = xformQuery.GetComponent(uid);

            var linVelocity = linearVelocities[offset + i];
            var physicsDirtied = false;

            if (!float.IsNaN(linVelocity.X) && !float.IsNaN(linVelocity.Y))
            {
                physicsDirtied |= SetLinearVelocity(uid, linVelocity, false, body: body);
            }

            var angVelocity = angularVelocities[offset + i];

            if (!float.IsNaN(angVelocity))
            {
                physicsDirtied |= SetAngularVelocity(uid, angVelocity, false, body: body);
            }

            // Temporary NaN guards until PVS is fixed.
            // May reparent object and change body's velocity.
            if (!float.IsNaN(position.X) && !float.IsNaN(position.Y))
            {
                _transform.SetLocalPositionRotation(uid,
                    xform.LocalPosition + position,
                    xform.LocalRotation + angle,
                    xform);
            }

            if (physicsDirtied)
                Dirty(uid, body);
        }
    }

    private void SleepBodies(in IslandData island, bool[] sleepStatus)
    {
        var offset = island.Offset;

        for (var i = 0; i < island.Bodies.Count; i++)
        {
            var sleep = sleepStatus[offset + i];

            if (!sleep)
                continue;

            var body = island.Bodies[i];

            SetAwake(body.Owner, body, false);
        }
    }
}
