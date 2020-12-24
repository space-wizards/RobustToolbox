using System;
using System.Collections.Generic;
using System.Diagnostics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Broadphase;
using Robust.Shared.Physics.Joints;
using Robust.Shared.Physics.Shapes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics
{
    public partial class PhysicsComponent : Component, IPhysBody
    {
        public override string Name => "Physics";
        public override uint? NetID => NetIDs.PHYSICS;

        // _xf is body origin transform, fucking WHAT

        internal bool Island { get; set; }

        internal int IslandIndex { get; set; }

        internal Sweep Sweep;

        public PhysicsMap? PhysicsMap { get; set; }

        // Aether2D uses these system-wide but for us we're just gonna store it on the component and call it a day.
        public HashSet<AetherController> Controllers { get; set; } = new HashSet<AetherController>();

        /// <summary>
        /// Gets or sets a value indicating whether this body should be included in the CCD solver.
        /// </summary>
        /// <value><c>true</c> if this instance is included in CCD; otherwise, <c>false</c>.</value>
        public bool IsBullet { get; set; }

        public bool IgnoreCCD { get; set; }

        /// <summary>
        /// Set this body to have fixed rotation. This causes the mass
        /// to be reset.
        /// </summary>
        /// <value><c>true</c> if it has fixed rotation; otherwise, <c>false</c>.</value>
        public bool FixedRotation
        {
            get => _fixedRotation;
            set
            {
                if (_fixedRotation == value)
                    return;

                _fixedRotation = value;

                _angularVelocity = 0f;
                ResetMassData();
            }
        }

        private bool _fixedRotation;

        public List<Fixture> FixtureList { get; set; } = new List<Fixture>();

        public ContactEdge? ContactList { get; set; }

        public JointEdge? JointList { get; internal set; }

        /// <summary>
        ///     We need to keep track of which grids we are relevant to for collision.
        /// </summary>
        private List<GridId> _grids = new List<GridId>();

        internal OnCollisionEventHandler? onCollisionEventHandler;

        public event OnCollisionEventHandler OnCollision
        {
            add => onCollisionEventHandler += value;
            remove => onCollisionEventHandler -= value;
        }

        internal OnSeparationEventHandler? onSeparationEventHandler;
        public event OnSeparationEventHandler OnSeparation
        {
            add => onSeparationEventHandler += value;
            remove => onSeparationEventHandler -= value;
        }

        /// <summary>
        ///     Angular velocity of this component in radians / second.
        /// </summary>
        public float AngularVelocity
        {
            get => _angularVelocity;
            set
            {
                if (BodyType == BodyType.Static || value == _angularVelocity)
                    return;

                if (value * value > 0f)
                    Awake = true;

                _angularVelocity = value;
            }
        }

        private float _angularVelocity;

        /// <summary>
        ///     Linear velocity of this component at its centre of mass.
        /// </summary>
        public Vector2 LinearVelocity
        {
            get => _linearVelocity;
            set
            {
                if (BodyType == BodyType.Static || value == _linearVelocity)
                    return;

                // TODO: Is this some 900IQ shit?
                if (Vector2.Dot(value, value) > 0f)
                    Awake = true;

                _linearVelocity = value;
            }
        }

        private Vector2 _linearVelocity = Vector2.Zero;

        public Vector2 Force { get; set; }

        /// <summary>
        ///     Rotational inertia about the local origin in kg / m ^ 2
        ///     Read-only during callbacks
        /// </summary>
        public float Inertia
        {
            get => _inertia + Mass * Vector2.Dot(Sweep.LocalCenter, Sweep.LocalCenter);
            set
            {
                // TODO: Debug assert
                if (BodyType != BodyType.Dynamic)
                    return;

                if (value > 0f && !_fixedRotation)
                {
                    _inertia = value - Mass * Vector2.Dot(LocalCentre, LocalCentre);
                    InvI = 1f / _inertia;
                }
            }
        }

        private float _inertia;

        /// <summary>
        ///     Inverse inertia
        /// </summary>
        public float InvI { get; private set; }

        /// <summary>
        ///     Local position of the center of mass
        /// </summary>
        public Vector2 LocalCentre { get; set; }

        /// <summary>
        ///     Inverse mass. Typically this is used over Mass.
        /// </summary>
        public float InvMass { get; set; }

        public float Mass
        {
            get => _mass;
            set
            {
                if (BodyType != BodyType.Dynamic)
                    return;

                _mass = value;

                if (_mass <= 0f)
                    _mass = 1f;

                InvMass = 1f / _mass;
            }
        }

        private float _mass;

        /// <summary>
        /// Get the local position of the center of mass.
        /// </summary>
        /// <value>The local position.</value>
        public Vector2 LocalCenter
        {
            get => Sweep.LocalCenter;
            set
            {
                if (_bodyType != BodyType.Dynamic)
                    return;

                // Move center of mass.
                Vector2 oldCenter = Sweep.Center;
                Sweep.LocalCenter = value;
                var transform = GetTransform();
                Sweep.Center0 = Sweep.Center = PhysicsMath.Multiply(ref Sweep.LocalCenter, ref transform);

                // Update center of mass velocity.
                Vector2 a = Sweep.Center - oldCenter;
                _linearVelocity += new Vector2(-_angularVelocity * a.Y, _angularVelocity * a.X);
            }
        }

        /// <summary>
        ///     How long have we been awake for to check if we can sleep.
        /// </summary>
        internal float SleepTime { get; set; }

        /// <summary>
        ///     https://en.wikipedia.org/wiki/Torque
        /// </summary>
        public float Torque { get; set; }

        public float LinearDamping { get; set; }

        public float AngularDamping { get; set; }

        /// <summary>
        ///     What type of body this, such as static or dynamic.
        ///     Readonly during callbacks.
        /// </summary>
        public BodyType BodyType
        {
            get => _bodyType;
            set
            {
                if (_bodyType == value)
                    return;

                _bodyType = value;

                ResetMassData();

                if (_bodyType == BodyType.Static)
                {
                    _linearVelocity = Vector2.Zero;
                    _angularVelocity = 0.0f;
                    Sweep.Angle0 = Sweep.Angle;
                    Sweep.Center0 = Sweep.Center;
                    SynchronizeFixtures();
                }

                Awake = true;

                // TODO: All the other shit
            }
        }

        private BodyType _bodyType;

        /// <summary>
        ///     Is this body enabled.
        /// </summary>
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (value == _enabled)
                    return;

                _enabled = value;

                if (_enabled)
                {
                    // TODO: If world not null create proxies?
                }
                else
                {
                    // TODO: If world not null destroy proxies + contacts.
                }
            }
        }

        private bool _enabled;

        /// <summary>
        ///     If the body is not allowed to sleep then it is always awake.
        /// </summary>
        public bool SleepingAllowed
        {
            get => _sleepingAllowed;
            set
            {
                if (!value)
                    Awake = true;

                _sleepingAllowed = value;
            }
        }

        private bool _sleepingAllowed = true;

        /// <summary>
        ///     Is this body active for physics?
        /// </summary>
        /// <remarks>
        ///     Asleep bodies are cheaper than awake ones.
        /// </remarks>
        public bool Awake
        {
            get => _awake;
            set
            {
                if (!_awake)
                {
                    SleepTime = 0f;
                    // TODO: Just use EventBus?
                    if (ContactList != null)
                        PhysicsMap?.ContactManager.UpdateActiveContacts(ContactList, true);

                    if (PhysicsMap?.AwakeBodySet.Contains(this) == false)
                        PhysicsMap.AwakeBodySet.Add(this);
                }
                else
                {
                    // Check even for BodyType.Static because if this body had just been changed to Static it will have
                    // set Awake = false in the process.
                    if (PhysicsMap?.AwakeBodySet.Contains(this) == true)
                        PhysicsMap.AwakeBodySet.Remove(this);

                    ResetDynamics();
                    if (ContactList != null)
                        PhysicsMap?.ContactManager.UpdateActiveContacts(ContactList, false);

                    SleepTime = 0f;
                }

                _awake = value;
            }
        }

        private bool _awake;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
        }

        // TODO: These 2 need testing
        public Vector2 GetLocalPoint(Vector2 worldPoint)
        {
            var transform = GetTransform();
            return PhysicsTransform.Divide(ref worldPoint, ref transform);
        }

        public Vector2 GetWorldPoint(Vector2 localPoint)
        {
            var transform = GetTransform();
            return PhysicsTransform.Multiply(ref localPoint, ref transform);
        }

        // TODO: This is just temporary so we can get aether2d off the ground faster and hopefully not needed in the future
        public PhysicsTransform GetTransform()
        {
            return new PhysicsTransform(Owner.Transform.WorldPosition, (float) Owner.Transform.WorldRotation.Theta);
        }

        /// <summary>
        /// Gets a local vector given a world vector.
        /// Note that the vector only takes the rotation into account, not the position.
        /// </summary>
        /// <param name="worldVector">A vector in world coordinates.</param>
        /// <returns>The corresponding local vector.</returns>
        public Vector2 GetLocalVector(Vector2 worldVector)
        {
            var transform = GetTransform();
            return Complex.Divide(worldVector, ref transform.Quaternion);
        }

        /// <summary>
        /// Get the world coordinates of a vector given the local coordinates.
        /// Note that the vector only takes the rotation into account, not the position.
        /// </summary>
        /// <param name="localVector">A vector fixed in the body.</param>
        /// <returns>The same vector expressed in world coordinates.</returns>
        public Vector2 GetWorldVector(Vector2 localVector)
        {
            var transform = GetTransform();
            return Complex.Multiply(localVector, ref transform.Quaternion);
        }

        public float Rotation => Sweep.Angle;

        /// <summary>
        /// This is used to prevent connected bodies from colliding.
        /// It may lie, depending on the collideConnected flag.
        /// </summary>
        /// <param name="other">The other body.</param>
        /// <returns></returns>
        public bool ShouldCollide(PhysicsComponent other)
        {
            // At least one body should be dynamic.
            if (_bodyType != BodyType.Dynamic && other._bodyType != BodyType.Dynamic)
            {
                return false;
            }

            // Does a joint prevent collision?
            for (JointEdge? jn = JointList; jn != null; jn = jn.Next)
            {
                if (jn.Other == other)
                {
                    if (jn.Joint.CollideConnected == false)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Resets the dynamics of this body.
        /// Sets torque, force and linear/angular velocity to 0
        /// </summary>
        public void ResetDynamics()
        {
            Torque = 0;
            _angularVelocity = 0;
            Force = Vector2.Zero;
            _linearVelocity = Vector2.Zero;
        }

        internal void Advance(float alpha)
        {
            // Advance to the new safe time. This doesn't sync the broad-phase.
            Sweep.Advance(alpha);
            Sweep.Center = Sweep.Center0;
            Sweep.Angle = Sweep.Angle0;
            Owner.Transform.WorldRotation = new Angle(Sweep.Angle);
            var quaternion = GetTransform().Quaternion;
            Owner.Transform.WorldPosition = Sweep.Center - Complex.Multiply(Sweep.LocalCenter, ref quaternion);
        }

        internal void SynchronizeTransform()
        {
            // TODO: Hope this shit works
            var transform = GetTransform();
            //transform.Quaternion.Phase = Sweep.Angle;
            Owner.Transform.WorldRotation = new Angle(Sweep.Angle);

            Owner.Transform.WorldPosition =
                Sweep.Center - Complex.Multiply(Sweep.LocalCenter, ref transform.Quaternion);

            // OG here just in case.
            //_xf.q.Phase = _sweep.A;
            //_xf.p = _sweep.C - Complex.Multiply(ref _sweep.LocalCenter, ref _xf.q);
        }

        /// <summary>
        /// For teleporting a body without considering new contacts immediately.
        /// Warning: This method is locked during callbacks.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="angle">The angle.</param>
        /// <exception cref="System.InvalidOperationException">Thrown when the world is Locked/Stepping.</exception>
        public void SetTransformIgnoreContacts(ref Vector2 position, float angle)
        {
            Debug.Assert(PhysicsMap != null);
            if (PhysicsMap.IsLocked)
                throw new InvalidOperationException("The World is locked.");

            // Same as SynchronizeTransform comment
            Owner.Transform.WorldRotation = new Angle(angle);
            Owner.Transform.WorldPosition = position;

            var transform = GetTransform();
            Sweep.Center = PhysicsMath.Multiply(ref Sweep.LocalCenter, ref transform);
            Sweep.Angle = angle;

            Sweep.Center0 = Sweep.Center;
            Sweep.Angle0 = angle;

            IoCManager.Resolve<IBroadPhaseManager>().SynchronizeFixtures(this, transform, transform);
        }

        /// <summary>
        ///     Resets the mass of this component to the sum of the fixtures.
        ///     You don't normally need to call this unless you have directly edited this component's mass.
        /// </summary>
        public void ResetMassData()
        {
            // Reset all of our stuff then re-sum our fixtures.
            _mass = 0f;
            InvMass = 0f;
            _inertia = 0f;
            InvI = 0f;

            if (BodyType == BodyType.Kinematic)
            {
                var worldPos = Owner.Transform.WorldPosition;

                Sweep.Center0 = worldPos;
                Sweep.Center = worldPos;
                Sweep.Angle0 = Sweep.Angle;
                return;
            }

            DebugTools.Assert(BodyType == BodyType.Dynamic || BodyType == BodyType.Static);

            var localCenter = Vector2.Zero;
            foreach (var fixture in FixtureList)
            {
                if (fixture.Shape.Density == 0f)
                    continue;

                var massData = fixture.Shape.MassData;
                _mass += massData.Mass;
                localCenter += massData.Centroid * massData.Mass;
                _inertia += massData.Inertia;
            }

            // OG comment from farseer here.
            //FPE: Static bodies only have mass, they don't have other properties. A little hacky tho...
            if (BodyType == BodyType.Static)
            {
                Sweep.Center0 = Sweep.Center = Owner.Transform.WorldPosition;
                return;
            }

            if (_mass > 0f)
            {
                InvMass = 1f / _mass;
                localCenter *= InvMass;
            }
            else
            {
                // Dynamic needs a minimum mass or shit will explode
                InvMass = 1f;
                _mass = 1f;
            }

            if (_inertia > 0f && !_fixedRotation)
            {
                // Centre inertia around centre of mass
                _inertia -= _mass * Vector2.Dot(localCenter, localCenter);

                DebugTools.Assert(_inertia > 0f);
                InvI = 1f / _inertia;
            }
            else
            {
                _inertia = 0f;
                InvI = 0f;
            }

            // Move centre of mass and update center of mass velocity
            var oldCenter = Sweep.Center;
            Sweep.LocalCenter = localCenter;
            // TODO: Replace this
            var transform = GetTransform();
            Sweep.Center0 = Sweep.Center = PhysicsMath.Multiply(ref Sweep.LocalCenter, ref transform);

            var a = Sweep.Center - oldCenter;
            _linearVelocity += new Vector2(-_angularVelocity * a.Y, _angularVelocity * a.X);
        }

        #region Fixtures
        /// <summary>
        /// Warning: This method is locked during callbacks.
        /// </summary>>
        /// <exception cref="System.InvalidOperationException">Thrown when the world is Locked/Stepping.</exception>
        public void AddFixture(Fixture fixture)
        {
            if (PhysicsMap != null && PhysicsMap.IsLocked)
                throw new InvalidOperationException("The World is locked.");
            if (fixture == null)
                throw new ArgumentNullException("fixture");

            fixture.Body = this;
            FixtureList.Add(fixture);
            // TODO Wat dis
#if DEBUG
            if (fixture.Shape.ShapeType == ShapeType.Polygon)
                ((PolygonShape) fixture.Shape).Vertices.AttachedToBody = true;
#endif

            // Adjust mass properties if needed.
            if (fixture.Shape.Density > 0.0f)
                ResetMassData();

            if (PhysicsMap != null)
            {
                if (Enabled)
                {
                    IoCManager.Resolve<IBroadPhaseManager>().CreateProxies(fixture);
                }

                // Let the world know we have a new fixture. This will cause new contacts
                // to be created at the beginning of the next time step.
                PhysicsMap._worldHasNewFixture = true;
            }
        }

        /// <summary>
        /// Destroy a fixture. This removes the fixture from the broad-phase and
        /// destroys all contacts associated with this fixture. This will
        /// automatically adjust the mass of the body if the body is dynamic and the
        /// fixture has positive density.
        /// All fixtures attached to a body are implicitly destroyed when the body is destroyed.
        /// Warning: This method is locked during callbacks.
        /// </summary>
        /// <param name="fixture">The fixture to be removed.</param>
        /// <exception cref="System.InvalidOperationException">Thrown when the world is Locked/Stepping.</exception>
        public virtual void RemoveFixture(Fixture fixture)
        {
            if (PhysicsMap != null && PhysicsMap.IsLocked)
                throw new InvalidOperationException("The World is locked.");
            if (fixture.Body != this)
                throw new ArgumentException("You are removing a fixture that does not belong to this Body.", "fixture");

            // Destroy any contacts associated with the fixture.
            ContactEdge? edge = ContactList;
            while (edge != null)
            {
                Debug.Assert(edge.Contact != null);
                Contact c = edge.Contact;
                Debug.Assert(c != null);
                edge = edge?.Next;

                Fixture? fixtureA = c.FixtureA;
                Fixture? fixtureB = c.FixtureB;

                if (fixture == fixtureA || fixture == fixtureB)
                {
                    // This destroys the contact and removes it from
                    // this body's contact list.
                    PhysicsMap?.ContactManager.Destroy(c);
                }
            }

            if (Enabled)
            {
                IoCManager.Resolve<IBroadPhaseManager>().DestroyProxies(fixture);
            }

            // TODO? fixture.Body = null;
            FixtureList.Remove(fixture);
#if DEBUG
            if (fixture.Shape.ShapeType == ShapeType.Polygon)
                ((PolygonShape)fixture.Shape).Vertices.AttachedToBody = false;
#endif
            ResetMassData();
        }

        #endregion
        #region Joints
        /// <summary>
        /// Create a joint to constrain bodies together. This may cause the connected bodies to cease colliding.
        /// Warning: This method is locked during callbacks.
        /// </summary>
        /// <param name="joint">The joint.</param>
        /// <exception cref="System.InvalidOperationException">Thrown when the world is Locked/Stepping.</exception>
        public void AddJoint(Joint joint)
        {
            PhysicsMap?.Add(joint);
        }

        public void RemoveJoint(Joint joint)
        {
            PhysicsMap?.Remove(joint);
        }
        #endregion

        public List<FixtureProxy> GetProxies(GridId gridId)
        {
            var proxies = new List<FixtureProxy>();
            foreach (var fixture in FixtureList)
            {
                foreach (var proxy in fixture.Proxies[gridId])
                {
                    proxies.Add(proxy);
                }
            }

            return proxies;
        }

        /// <summary>
         ///    Create all proxies.
         /// </summary>
        internal void CreateProxies()
        {
            IoCManager.Resolve<IBroadPhaseManager>().AddBody(this);
        }

        internal void DestroyProxies()
        {
            IoCManager.Resolve<IBroadPhaseManager>().RemoveBody(this);
        }

        internal void SynchronizeFixtures()
        {
            PhysicsTransform xf1 = new PhysicsTransform(Vector2.Zero, Sweep.Angle0);
            xf1.Position = Sweep.Center0 - Complex.Multiply(Sweep.LocalCenter, ref xf1.Quaternion);
            IoCManager.Resolve<IBroadPhaseManager>().SynchronizeFixtures(this, xf1, GetTransform());
        }
    }
}
