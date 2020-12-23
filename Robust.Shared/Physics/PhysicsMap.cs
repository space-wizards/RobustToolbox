using System;
using System.Collections.Generic;
using System.Diagnostics;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Joints;

namespace Robust.Shared.Physics
{
    /// <summary>
    ///     All of the physics components on a particular map.
    /// </summary>
    /// <remarks>
    ///     What you'd call a "World" in some other engines.
    /// </remarks>
    public class PhysicsMap
    {
        // TODO: Licences on all the shit.

        #region These are for debugging the solver.
        /// <summary>This is only for debugging the solver</summary>
        private const bool _warmStarting = true;
        /// <summary>This is only for debugging the solver</summary>
        private const bool _subStepping = false;
        #endregion

        Vector2 _gravity;

        private bool _stepComplete = true;

        private float _invDt0;
        private PhysicsComponent[] _stack = new PhysicsComponent[64];

        private TOIInput _input = new TOIInput();

        public MapId MapId { get; set; }

        internal bool _worldHasNewFixture;

        /// <summary>
        /// Change the global gravity vector.
        /// </summary>
        /// <value>The gravity.</value>
        public Vector2 Gravity
        {
            get { return _gravity; }
            set
            {
                if (IsLocked)
                    throw new InvalidOperationException("The World is locked.");
                _gravity = value;
            }
        }

        /// <summary>
        /// Is the world locked (in the middle of a time step).
        /// </summary>
        public bool IsLocked { get; private set; }

        /// <summary>
        /// Get the contact manager for testing.
        /// </summary>
        /// <value>The contact manager.</value>
        public readonly ContactManager ContactManager;

        /// <summary>
        /// Get the world body list.
        /// </summary>
        /// <value>The head of the world body list.</value>
        public readonly List<PhysicsComponent> BodyList;

        public readonly List<Joint> JointList;

        public HashSet<PhysicsComponent> AwakeBodySet { get; private set; }
        List<PhysicsComponent> AwakeBodyList;
        HashSet<PhysicsComponent> IslandSet;
        HashSet<PhysicsComponent> TOISet;

        /// <summary>
        /// If false, the whole simulation stops. It still processes added and removed geometries.
        /// </summary>
        public bool Enabled { get; set; }

        public PhysicsIsland Island { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="World"/> class.
        /// </summary>
        public PhysicsMap()
        {
            Island = new PhysicsIsland();
            Enabled = true;
            BodyList = new List<PhysicsComponent>(32);
            JointList = new List<Joint>(32);

            AwakeBodySet = new HashSet<PhysicsComponent>();
            AwakeBodyList = new List<PhysicsComponent>(32);
            IslandSet = new HashSet<PhysicsComponent>();
            TOISet = new HashSet<PhysicsComponent>();

            ContactManager = new ContactManager();
            Gravity = new Vector2(0f, -9.80665f);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="World"/> class.
        /// </summary>
        /// <param name="gravity">The gravity.</param>
        public PhysicsMap(Vector2 gravity) : this()
        {
            Gravity = gravity;
        }

        private void Solve(ref PhysicsStep step)
        {
            // Sloth notes:
            /*
             * Soooo I used some of the preprocessors like USE_ISLAND_SET and _USE_AWAKE_BODY_SET
             * buuuttt some of these don't actually compile in aether2d so :)
             */

            // Size the island for the worst case.
            Island.Reset(BodyList.Count,
                         ContactManager.ContactCount,
                         8,
                         ContactManager);

            // Clear all the island flags.
            Debug.Assert(IslandSet.Count == 0);

            // TODO: Go through each active contact and set its island flag to false
            // TODO: Go through each joint and set its flag to false
            // Maybe TODO: Go through each body and set its island flag to false?

            // Build and simulate all awake islands.
            int stackSize = BodyList.Count;
            if (stackSize > _stack.Length)
                _stack = new PhysicsComponent[Math.Max(_stack.Length * 2, stackSize)];

            // If AwakeBodyList is empty, the Island code will not have a chance
            // to update the diagnostics timer so reset the timer here.
            Island.JointUpdateTime = TimeSpan.Zero;

            Debug.Assert(AwakeBodyList.Count == 0);
            AwakeBodyList.AddRange(AwakeBodySet);

            foreach (var seed in AwakeBodyList)
            {
                if (seed.Island)
                {
                    continue;
                }

                if (seed.Awake == false || seed.Enabled == false)
                {
                    continue;
                }

                // The seed can be dynamic or kinematic.
                if (seed.BodyType == BodyType.Static)
                {
                    continue;
                }

                // Reset island and stack.
                Island.Clear();
                int stackCount = 0;
                _stack[stackCount++] = seed;

                if (!IslandSet.Contains(seed))
                    IslandSet.Add(seed);

                seed.Island = true;

                // Perform a depth first search (DFS) on the constraint graph.
                while (stackCount > 0)
                {
                    // Grab the next body off the stack and add it to the island.
                    PhysicsComponent body = _stack[--stackCount];
                    Debug.Assert(body.Enabled);
                    Island.Add(body);

                    // Make sure the body is awake.
                    body.Awake = true;

                    // To keep islands as small as possible, we don't
                    // propagate islands across static bodies.
                    if (body.BodyType == BodyType.Static)
                    {
                        continue;
                    }

                    // Search all contacts connected to this body.
                    for (ContactEdge? ce = body.ContactList; ce != null; ce = ce.Next)
                    {
                        Contact? contact = ce.Contact;
                        Debug.Assert(contact != null && contact != null);

                        // Has this contact already been added to an island?
                        if (contact.IslandFlag)
                        {
                            continue;
                        }

                        // Is this contact solid and touching?
                        if (contact.Enabled == false || contact.IsTouching == false)
                        {
                            continue;
                        }

                        // Skip sensors.
                        bool? sensorA = contact.FixtureA?.IsSensor;
                        bool? sensorB = contact.FixtureB?.IsSensor;
                        if (sensorA == true || sensorB == true)
                        {
                            continue;
                        }

                        Island.Add(contact);
                        contact.IslandFlag = true;

                        PhysicsComponent? other = ce.Other;

                        // Was the other body already added to this island?
                        if (other == null || other.Island)
                        {
                            continue;
                        }

                        Debug.Assert(stackCount < stackSize);
                        _stack[stackCount++] = other;

                        if (!IslandSet.Contains(body))
                            IslandSet.Add(body);

                        other.Island = true;
                    }

                    // Search all joints connect to this body.
                    for (JointEdge? je = body.JointList; je != null; je = je.Next)
                    {
                        if (je.Joint.IslandFlag)
                        {
                            continue;
                        }

                        PhysicsComponent? other = je.Other;

                        // WIP David
                        //Enter here when it's a non-fixed joint. Non-fixed joints have a other body.
                        if (other != null)
                        {
                            // Don't simulate joints connected to inactive bodies.
                            if (other.Enabled == false)
                            {
                                continue;
                            }

                            Island.Add(je.Joint);
                            je.Joint.IslandFlag = true;

                            if (other.Island)
                            {
                                continue;
                            }

                            Debug.Assert(stackCount < stackSize);
                            _stack[stackCount++] = other;
                            if (!IslandSet.Contains(body))
                                IslandSet.Add(body);

                            other.Island = true;
                        }
                        else
                        {
                            Island.Add(je.Joint);
                            je.Joint.IslandFlag = true;
                        }
                    }
                }

                Island.Solve(step);

                // Post solve cleanup.
                for (int i = 0; i < Island.BodyCount; ++i)
                {
                    // Allow static bodies to participate in other islands.
                    PhysicsComponent b = Island.Bodies[i];
                    if (b.BodyType == BodyType.Static)
                    {
                        b.Island = false;
                    }
                }
            }

            // Synchronize fixtures, check for out of range bodies.
            foreach (var b in IslandSet)
            {
                if (!TOISet.Contains(b))
                    TOISet.Add(b);

                // If a body was not in an island then it did not move.
                if (!b.Island)
                {
                    continue;
                }
                Debug.Assert(b.BodyType != BodyType.Static);

                // Update fixtures (for broad-phase).
                b.SynchronizeFixtures();
            }

            IslandSet.Clear();

            // Look for new contacts.
            ContactManager.FindNewContacts(MapId);

            AwakeBodyList.Clear();
        }

        private void SolveTOI(ref PhysicsStep step, ref SolverIterations iterations)
        {
            Island.Reset(2 * PhysicsSettings.MaxTOIContacts, PhysicsSettings.MaxTOIContacts, 0, ContactManager);

            bool wasStepComplete = _stepComplete;

            if (_stepComplete)
            {
                foreach (var b in TOISet)
                {
                    // Sloth: Original uses BodyFlags but the compilable version just uses the Island bool
                    b.Island = false;
                    b.Sweep.Alpha0 = 0.0f;
                }

                foreach (var c in ContactManager.ActiveContacts)
                {
                    // Invalidate TOI
                    c.IslandFlag = false;
                    c.TOIFlag = false;
                    c._toiCount = 0;
                    c._toi = 1.0f;
                }
            }

            // Find TOI events and solve them.
            for (; ; )
            {
                // Find the first TOI.
                Contact? minContact = null;
                float minAlpha = 1.0f;

                foreach (var c in ContactManager.ActiveContacts)
                {
                    // Is this contact disabled?
                    if (c.Enabled == false)
                    {
                        continue;
                    }

                    // Prevent excessive sub-stepping.
                    if (c._toiCount > PhysicsSettings.MaxSubSteps)
                    {
                        continue;
                    }

                    float alpha;
                    if (c.TOIFlag)
                    {
                        // This contact has a valid cached TOI.
                        alpha = c._toi;
                    }
                    else
                    {
                        Fixture? fA = c.FixtureA;
                        Fixture? fB = c.FixtureB;

                        Debug.Assert(fA != null && fB != null);

                        // Is there a sensor?
                        if (fA.IsSensor || fB.IsSensor)
                        {
                            continue;
                        }

                        PhysicsComponent bA = fA.Body;
                        PhysicsComponent bB = fB.Body;

                        BodyType typeA = bA.BodyType;
                        BodyType typeB = bB.BodyType;
                        Debug.Assert(typeA == BodyType.Dynamic || typeB == BodyType.Dynamic);

                        bool activeA = bA.Awake && typeA != BodyType.Static;
                        bool activeB = bB.Awake && typeB != BodyType.Static;

                        // Is at least one body active (awake and dynamic or kinematic)?
                        if (activeA == false && activeB == false)
                        {
                            continue;
                        }

                        bool collideA = (bA.IsBullet || typeA != BodyType.Dynamic) && !bA.IgnoreCCD;
                        bool collideB = (bB.IsBullet || typeB != BodyType.Dynamic) && !bB.IgnoreCCD;

                        // Are these two non-bullet dynamic bodies?
                        if (collideA == false && collideB == false)
                        {
                            continue;
                        }

                        if (_stepComplete)
                        {
                            if (!TOISet.Contains(bA))
                            {
                                TOISet.Add(bA);
                                bA.Island = false;
                                bA.Sweep.Alpha0 = 0.0f;
                            }
                            if (!TOISet.Contains(bB))
                            {
                                TOISet.Add(bB);
                                bB.Island = false;
                                bB.Sweep.Alpha0 = 0.0f;
                            }
                        }

                        // Compute the TOI for this contact.
                        // Put the sweeps onto the same time interval.
                        float alpha0 = bA.Sweep.Alpha0;

                        if (bA.Sweep.Alpha0 < bB.Sweep.Alpha0)
                        {
                            alpha0 = bB.Sweep.Alpha0;
                            bA.Sweep.Advance(alpha0);
                        }
                        else if (bB.Sweep.Alpha0 < bA.Sweep.Alpha0)
                        {
                            alpha0 = bA.Sweep.Alpha0;
                            bB.Sweep.Advance(alpha0);
                        }

                        Debug.Assert(alpha0 < 1.0f);

                        // Compute the time of impact in interval [0, minTOI]
                        _input.ProxyA = new DistanceProxy(fA.Shape, c.ChildIndexA);
                        _input.ProxyB = new DistanceProxy(fB.Shape, c.ChildIndexB);
                        _input.SweepA = bA.Sweep;
                        _input.SweepB = bB.Sweep;
                        _input.TMax = 1.0f;

                        TOIOutput output;
                        TimeOfImpact.CalculateTimeOfImpact(out output, ref _input);

                        // Beta is the fraction of the remaining portion of the .
                        float beta = output.T;
                        if (output.State == TOIOutputState.Touching)
                        {
                            alpha = Math.Min(alpha0 + (1.0f - alpha0) * beta, 1.0f);
                        }
                        else
                        {
                            alpha = 1.0f;
                        }

                        c._toi = alpha;
                        c.TOIFlag = true;
                    }

                    if (alpha < minAlpha)
                    {
                        // This is the minimum TOI found so far.
                        minContact = c;
                        minAlpha = alpha;
                    }
                }

                if (minContact == null || 1.0f - 10.0f * float.Epsilon < minAlpha)
                {
                    // No more TOI events. Done!
                    _stepComplete = true;
                    break;
                }

                // Advance the bodies to the TOI.
                Debug.Assert(minContact.FixtureA != null && minContact.FixtureB != null);
                Fixture fA1 = minContact.FixtureA;
                Fixture fB1 = minContact.FixtureB;
                PhysicsComponent bA0 = fA1.Body;
                PhysicsComponent bB0 = fB1.Body;

                Sweep backup1 = bA0.Sweep;
                Sweep backup2 = bB0.Sweep;

                bA0.Advance(minAlpha);
                bB0.Advance(minAlpha);

                // The TOI contact likely has some new contact points.
                minContact.Update(ContactManager);
                minContact.TOIFlag = false;
                ++minContact._toiCount;

                // Is the contact solid?
                if (minContact.Enabled == false || minContact.IsTouching == false)
                {
                    // Restore the sweeps.
                    minContact.Enabled = false;
                    bA0.Sweep = backup1;
                    bB0.Sweep = backup2;
                    bA0.SynchronizeTransform();
                    bB0.SynchronizeTransform();
                    continue;
                }

                bA0.Awake = true;
                bB0.Awake = true;

                // Build the island
                Island.Clear();
                Island.Add(bA0);
                Island.Add(bB0);
                Island.Add(minContact);

                bA0.Island = true;
                bB0.Island = true;
                minContact.IslandFlag = true;

                // Get contacts on bodyA and bodyB.
                PhysicsComponent[] bodies = { bA0, bB0 };
                for (int i = 0; i < 2; ++i)
                {
                    PhysicsComponent body = bodies[i];
                    if (body.BodyType == BodyType.Dynamic)
                    {
                        for (ContactEdge? ce = body.ContactList; ce != null; ce = ce?.Next)
                        {
                            Contact? contact = ce?.Contact;
                            Debug.Assert(ce != null && contact != null);

                            if (Island.BodyCount == Island.BodyCapacity)
                            {
                                break;
                            }

                            if (Island.ContactCount == Island.ContactCapacity)
                            {
                                break;
                            }

                            // Has this contact already been added to the island?
                            if (contact.IslandFlag)
                            {
                                continue;
                            }

                            // Only add static, kinematic, or bullet bodies.
                            PhysicsComponent? other = ce.Other;
                            if (other?.BodyType == BodyType.Dynamic &&
                                body.IsBullet == false && other.IsBullet == false)
                            {
                                continue;
                            }

                            // Skip sensors.
                            if (contact.FixtureA?.IsSensor == true || contact.FixtureB?.IsSensor == true)
                            {
                                continue;
                            }

                            // Tentatively advance the body to the TOI.
                            Debug.Assert(other != null);
                            Sweep backup = other.Sweep;
                            if (!other.Island)
                            {
                                other.Advance(minAlpha);
                            }

                            // Update the contact points
                            contact.Update(ContactManager);

                            // Was the contact disabled by the user?
                            if (contact.Enabled == false)
                            {
                                other.Sweep = backup;
                                other.SynchronizeTransform();
                                continue;
                            }

                            // Are there contact points?
                            if (contact.IsTouching == false)
                            {
                                other.Sweep = backup;
                                other.SynchronizeTransform();
                                continue;
                            }

                            // Add the contact to the island
                            contact.IslandFlag = true;
                            Island.Add(contact);

                            // Has the other body already been added to the island?
                            if (other.Island)
                            {
                                continue;
                            }

                            // Add the other body to the island.
                            other.Island = true;

                            if (other.BodyType != BodyType.Static)
                            {
                                other.Awake = true;
                            }

                            if (_stepComplete)
                            {
                                if (!TOISet.Contains(other))
                                {
                                    TOISet.Add(other);
                                    other.Sweep.Alpha0 = 0.0f;
                                }
                            }

                            Island.Add(other);
                        }
                    }
                }

                PhysicsStep subStep;
                subStep.PositionIterations = iterations.TOIPositionIterations;
                subStep.VelocityIterations = iterations.TOIVelocityIterations;
                subStep.DeltaTime = (1.0f - minAlpha) * step.DeltaTime;
                subStep.InvDt = 1.0f / subStep.DeltaTime;
                subStep.DtRatio = 1.0f;
                subStep.WarmStarting = false;
                Island.SolveTOI(ref subStep, bA0.IslandIndex, bB0.IslandIndex);

                // Reset island flags and synchronize broad-phase proxies.
                for (int i = 0; i < Island.BodyCount; ++i)
                {
                    PhysicsComponent body = Island.Bodies[i];
                    body.Island = false;

                    if (body.BodyType != BodyType.Dynamic)
                    {
                        continue;
                    }

                    body.SynchronizeFixtures();

                    // Invalidate all contact TOIs on this displaced body.
                    for (ContactEdge? ce = body.ContactList; ce != null; ce = ce.Next)
                    {
                        Debug.Assert(ce.Contact != null);
                        ce.Contact.TOIFlag = false;
                        ce.Contact.IslandFlag = false;
                    }
                }

                // Commit fixture proxy movements to the broad-phase so that new contacts are created.
                // Also, some contacts can be destroyed.
                ContactManager.FindNewContacts(MapId);

                if (_subStepping)
                {
                    _stepComplete = false;
                    break;
                }
            }

            if (wasStepComplete)
                TOISet.Clear();
        }

        #region Body
        public void Add(PhysicsComponent body)
        {
            if (body.PhysicsMap == this)
                throw new ArgumentException("You are adding the same body more than once.", "body");
            if (body.PhysicsMap != null)
                throw new ArgumentException("body belongs to another world.", "body");

            if (body.Awake)
            {
                // TODO: What is this hashset bullshit on adding
                if (!AwakeBodySet.Contains(body))
                    AwakeBodySet.Add(body);
            }
            else
            {
                if (AwakeBodySet.Contains(body))
                    AwakeBodySet.Remove(body);
            }

            body.PhysicsMap = this;
            BodyList.Add(body);

            // Update transform
            var position = body.Owner.Transform.WorldPosition;
            body.SetTransformIgnoreContacts(ref position, body.Rotation);

            // Create proxies
            // TODO: Check if Proxies exist
            if (Enabled)
            {
                body.CreateProxies();
            }

            ContactManager.FindNewContacts(MapId);
        }

        public virtual void Remove(PhysicsComponent body)
        {
            if (body.PhysicsMap != this)
                throw new ArgumentException("You are removing a body that is not in the simulation.", "body");

            Debug.Assert(!AwakeBodySet.Contains(body));

            // Delete the attached joints.
            JointEdge? je = body.JointList;
            while (je != null)
            {
                JointEdge je0 = je;
                je = je.Next;

                Remove(je0.Joint);
            }

            body.JointList = null;

            // Delete the attached contacts.
            ContactEdge? ce = body.ContactList;
            while (ce != null)
            {
                ContactEdge ce0 = ce;
                ce = ce.Next;
                Debug.Assert(ce0.Contact != null);
                ContactManager.Destroy(ce0.Contact);
            }
            body.ContactList = null;

            // remove the attached contact callbacks
            body.onCollisionEventHandler = null;
            body.onSeparationEventHandler = null;

            // Delete the attached fixtures. This destroys broad-phase proxies.
            body.DestroyProxies();
            body.PhysicsMap = null;
            BodyList.Remove(body);

            Debug.Assert(!AwakeBodySet.Contains(body));
        }
        #endregion

        #region Joints
        /// <summary>
        /// Create a joint to constrain bodies together. This may cause the connected bodies to cease colliding.
        /// Warning: This method is locked during callbacks.
        /// </summary>
        /// <param name="joint">The joint.</param>
        /// <exception cref="System.InvalidOperationException">Thrown when the world is Locked/Stepping.</exception>
        public void Add(Joint joint)
        {
            if (IsLocked)
                throw new InvalidOperationException("The World is locked.");
            if (joint == null)
                throw new ArgumentNullException("joint");
            if (JointList.Contains(joint))
                throw new ArgumentException("You are adding the same joint more than once.", "joint");

            // Connect to the world list.
            JointList.Add(joint);

            // Connect to the bodies' doubly linked lists.
            joint.EdgeA.Joint = joint;
            joint.EdgeA.Other = joint.BodyB;
            joint.EdgeA.Prev = null;
            joint.EdgeA.Next = joint.BodyA.JointList;

            if (joint.BodyA.JointList != null)
                joint.BodyA.JointList.Prev = joint.EdgeA;

            joint.BodyA.JointList = joint.EdgeA;

            // WIP David
            if (!joint.IsFixedType())
            {
                joint.EdgeB.Joint = joint;
                joint.EdgeB.Other = joint.BodyA;
                joint.EdgeB.Prev = null;
                joint.EdgeB.Next = joint.BodyB.JointList;

                if (joint.BodyB.JointList != null)
                    joint.BodyB.JointList.Prev = joint.EdgeB;

                joint.BodyB.JointList = joint.EdgeB;

                PhysicsComponent bodyA = joint.BodyA;
                PhysicsComponent bodyB = joint.BodyB;

                // If the joint prevents collisions, then flag any contacts for filtering.
                if (joint.CollideConnected == false)
                {
                    ContactEdge? edge = bodyB.ContactList;

                    while (edge != null)
                    {
                        if (edge.Other == bodyA)
                        {
                            // Flag the contact for filtering at the next time step (where either
                            // body is awake).
                            Debug.Assert(edge.Contact != null);
                            edge.Contact.FilterFlag = true;
                        }

                        edge = edge.Next;
                    }
                }
            }
            // Note: creating a joint doesn't wake the bodies.
        }

        /// <summary>
        /// Destroy a joint. This may cause the connected bodies to begin colliding.
        /// Warning: This method is locked during callbacks.
        /// </summary>
        /// <param name="joint">The joint.</param>
        /// <exception cref="System.InvalidOperationException">Thrown when the world is Locked/Stepping.</exception>
        public void Remove(Joint joint)
        {
            if (IsLocked)
                throw new InvalidOperationException("The World is locked.");
            if (joint == null)
                throw new ArgumentNullException("joint");
            if (!JointList.Contains(joint))
                throw new ArgumentException("You are removing a joint that is not in the simulation.", "joint");

            bool collideConnected = joint.CollideConnected;

            // Remove from the world list.
            JointList.Remove(joint);

            // Disconnect from island graph.
            PhysicsComponent bodyA = joint.BodyA;
            PhysicsComponent bodyB = joint.BodyB;

            // Wake up connected bodies.
            bodyA.Awake = true;

            // WIP David
            if (!joint.IsFixedType())
            {
                bodyB.Awake = true;
            }

            // Remove from body 1.
            if (joint.EdgeA.Prev != null)
            {
                joint.EdgeA.Prev.Next = joint.EdgeA.Next;
            }

            if (joint.EdgeA.Next != null)
            {
                joint.EdgeA.Next.Prev = joint.EdgeA.Prev;
            }

            if (joint.EdgeA == bodyA.JointList)
            {
                bodyA.JointList = joint.EdgeA.Next;
            }

            joint.EdgeA.Prev = null;
            joint.EdgeA.Next = null;

            // WIP David
            if (!joint.IsFixedType())
            {
                // Remove from body 2
                if (joint.EdgeB.Prev != null)
                {
                    joint.EdgeB.Prev.Next = joint.EdgeB.Next;
                }

                if (joint.EdgeB.Next != null)
                {
                    joint.EdgeB.Next.Prev = joint.EdgeB.Prev;
                }

                if (joint.EdgeB == bodyB.JointList)
                {
                    bodyB.JointList = joint.EdgeB.Next;
                }

                joint.EdgeB.Prev = null;
                joint.EdgeB.Next = null;
            }

            // WIP David
            if (!joint.IsFixedType())
            {
                // If the joint prevents collisions, then flag any contacts for filtering.
                if (collideConnected == false)
                {
                    ContactEdge? edge = bodyB.ContactList;
                    while (edge != null)
                    {
                        if (edge.Other == bodyA)
                        {
                            // Flag the contact for filtering at the next time step (where either
                            // body is awake).
                            Debug.Assert(edge.Contact != null);
                            edge.Contact.FilterFlag = true;
                        }

                        edge = edge.Next;
                    }
                }
            }
        }
        #endregion

        /// <summary>
        /// Take a time step. This performs collision detection, integration,
        /// and consraint solution.
        /// Warning: This method is locked during callbacks.
        /// </summary>
        /// <param name="dt">The amount of time to simulate in seconds, this should not vary.</param>
        /// <exception cref="System.InvalidOperationException">Thrown when the world is Locked/Stepping.</exception>
        public void Step(float dt)
        {
            var iterations = new SolverIterations
            {
                PositionIterations = PhysicsSettings.PositionIterations,
                VelocityIterations = PhysicsSettings.VelocityIterations,
                TOIPositionIterations = PhysicsSettings.TOIPositionIterations,
                TOIVelocityIterations = PhysicsSettings.TOIVelocityIterations
            };

            Step(dt, ref iterations);
        }

        /// <summary>
        /// Take a time step. This performs collision detection, integration,
        /// and consraint solution.
        /// Warning: This method is locked during callbacks.
        /// </summary>
        /// <param name="dt">The amount of time to simulate in seconds, this should not vary.</param>
        /// <exception cref="System.InvalidOperationException">Thrown when the world is Locked/Stepping.</exception>
        public void Step(float dt, ref SolverIterations iterations)
        {
            if (IsLocked)
                throw new InvalidOperationException("The World is locked.");

            if (!Enabled)
                return;

            // If new fixtures were added, we need to find the new contacts.
            if (_worldHasNewFixture)
            {
                ContactManager.FindNewContacts(MapId);
                _worldHasNewFixture = false;
            }

            //FPE only: moved position and velocity iterations into PhysicsSettings.cs
            PhysicsStep step;
            step.PositionIterations = iterations.PositionIterations;
            step.VelocityIterations = iterations.VelocityIterations;
            step.DeltaTime = dt;
            step.InvDt = (dt > 0.0f) ? (1.0f / dt) : 0.0f;
            step.DtRatio = _invDt0 * dt;
            step.WarmStarting = _warmStarting;

            IsLocked = true;
            try
            {
                // Update controllers
                // Modified from Aether2D where it uses system-wide controllers
                foreach (var body in AwakeBodySet)
                {
                    foreach (var controller in body.Controllers)
                    {
                        controller.Update(dt);
                    }
                }

                // Update contacts. This is where some contacts are destroyed.
                ContactManager.Collide();

                // Integrate velocities, solve velocity constraints, and integrate positions.
                if (_stepComplete && step.DeltaTime > 0.0f)
                {
                    Solve(ref step);
                }

                // Handle TOI events.
                if (PhysicsSettings.ContinuousPhysics && step.DeltaTime > 0.0f)
                {
                    SolveTOI(ref step, ref iterations);
                }

                if (PhysicsSettings.AutoClearForces)
                    ClearForces();
            }
            finally
            {
                IsLocked = false;
            }

            if (step.DeltaTime > 0.0f)
                _invDt0 = step.InvDt;

        }

        /// <summary>
        /// Call this after you are done with time steps to clear the forces. You normally
        /// call this after each call to Step, unless you are performing sub-steps. By default,
        /// forces will be automatically cleared, so you don't need to call this function.
        /// </summary>
        public void ClearForces()
        {
            // TODO: What the fuck it's so inefficient

            for (int i = 0; i < BodyList.Count; i++)
            {
                PhysicsComponent body = BodyList[i];
                body.Force = Vector2.Zero;
                body.Torque = 0.0f;
            }
        }
    }
}
