using System;
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

    private record struct IslandData(int Index, bool LoneIsland, List<PhysicsComponent> Bodies, List<Contact> Contacts, List<Joint> Joints)
    {
        public readonly int Index = Index;
        public readonly bool LoneIsland = LoneIsland;
        public readonly List<PhysicsComponent> Bodies = Bodies;
        public readonly List<Contact> Contacts = Contacts;
        public readonly List<Joint> Joints = Joints;
    }

    // Caching for island generation.
    private readonly HashSet<PhysicsComponent> _islandSet = new(64);
    private readonly Stack<PhysicsComponent> _bodyStack = new(64);
    private readonly List<PhysicsComponent> _awakeBodyList = new(256);

    /// <summary>
    ///     Where the magic happens.
    /// </summary>
    /// <param name="frameTime"></param>
    /// <param name="prediction"></param>
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
            component.ClearForces();

        component._invDt0 = invDt;
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

        while (iBegin < islands.Count)
        {
            var island = islands[iBegin];

            if (!InternalParallel(island))
                break;

            SolveIsland(island, gravity, frameTime, dtRatio, invDt, prediction);
            iBegin++;
            // TODO: Submit rest in parallel if applicable
        }

        Parallel.For(iBegin, islands.Count, i =>
        {
            var island = islands[i];
            SolveIsland(island, gravity, frameTime, dtRatio, invDt, prediction);
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
        if (island.Contacts.Count > 32 || island.Joints.Count > 32)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Go through all the bodies in this island and solve.
    /// </summary>
    private void SolveIsland(IslandData island, Vector2 gravity, float frameTime, float dtRatio, float invDt, bool prediction)
    {
#if DEBUG
        RaiseLocalEvent(new IslandSolveMessage(island.Bodies));
#endif

        var bodyCount = island.Bodies.Count;
        Span<Vector2> positions = stackalloc Vector2[bodyCount];
        Span<Angle> angles = stackalloc Angle[bodyCount];
        Span<Vector2> linearVelocities = stackalloc Vector2[bodyCount];
        Span<float> angularVelocities = stackalloc float[bodyCount];

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

        var data = new SolverData()
        {
            FrameTime = frameTime,
            DtRatio = dtRatio,
        };

        // TODO: Do these up front of the world step.
        SolverData.InvDt = invDt;
        SolverData.IslandIndex = ID;
        SolverData.WarmStarting = _warmStarting;
        SolverData.MaxLinearCorrection = _maxLinearCorrection;
        SolverData.MaxAngularCorrection = _maxAngularCorrection;

        // Pass the data into the solver
        solver.Reset(data, island.Contacts);

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
                joint.InitVelocityConstraints(SolverData, bodyA, bodyB);
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
            if (translation.Length > _maxLinearVelocity)
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

            linearVelocities[i] = linearVelocity;
            angularVelocities[i] = angularVelocity;
            positions[i] = position;
            angles[i] = angle;
        }

        _positionSolved = false;

        for (var i = 0; i < _positionIterations; i++)
        {
            var contactsOkay = _contactSolver.SolvePositionConstraints();
            var jointsOkay = true;

            for (int j = 0; j < JointCount; ++j)
            {
                var joint = _joints[j].Joint;

                if (!joint.Enabled)
                    continue;

                var jointOkay = joint.SolvePositionConstraints(SolverData);

                jointsOkay = jointsOkay && jointOkay;
            }

            if (contactsOkay && jointsOkay)
            {
                _positionSolved = true;
                break;
            }
        }
    }

    internal void UpdateBodies(HashSet<TransformComponent> deferredUpdates)
    {
        foreach (var (joint, error) in _brokenJoints)
        {
            var msg = new Joint.JointBreakMessage(joint, MathF.Sqrt(error));
            var eventBus = _entityManager.EventBus;
            eventBus.RaiseLocalEvent(joint.BodyAUid, msg, false);
            eventBus.RaiseLocalEvent(joint.BodyBUid, msg, false);
            eventBus.RaiseEvent(EventSource.Local, msg);
        }

        _brokenJoints.Clear();

        var xforms = GetEntityQuery<TransformComponent>();

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
            var bodyPos = positions[i];
            var angle = angles[i];

            // Temporary NaN guards until PVS is fixed.
            if (!float.IsNaN(bodyPos.X) && !float.IsNaN(bodyPos.Y))
            {
                var q = new Quaternion2D(angle);

                bodyPos -= Transform.Mul(q, body.LocalCenter);
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
                _physics.SetLinearVelocity(body, linVelocity);
            }

            var angVelocity = _angularVelocities[i];

            if (!float.IsNaN(angVelocity))
            {
                _physics.SetAngularVelocity(body, angVelocity);
            }
        }
    }

    private void SleepBodies(IslandData island, bool prediction, float frameTime)
    {
        if (island.LoneIsland)
        {
            if (!prediction && _sleepAllowed)
            {
                for (var i = 0; i < island.Bodies.Count; i++)
                {
                    var body = island.Bodies[i];

                    if (body.BodyType == BodyType.Static) continue;

                    if (!body.SleepingAllowed ||
                        body.AngularVelocity * body.AngularVelocity > _angTolSqr ||
                        Vector2.Dot(body.LinearVelocity, body.LinearVelocity) > _linTolSqr)
                    {
                        SetSleepTime(body, 0f);
                    }
                    else
                    {
                        SetSleepTime(body, body.SleepTime + frameTime);
                    }

                    if (body.SleepTime >= _timeToSleep && _positionSolved)
                    {
                        SetAwake(body, false);
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

                for (var i = 0; i < island.Bodies.Count; i++)
                {
                    var body = island.Bodies[i];

                    if (body.BodyType == BodyType.Static) continue;

                    if (!body.SleepingAllowed ||
                        body.AngularVelocity * body.AngularVelocity > _angTolSqr ||
                        Vector2.Dot(body.LinearVelocity, body.LinearVelocity) > _linTolSqr)
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

                if (minSleepTime >= _timeToSleep && _positionSolved)
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
