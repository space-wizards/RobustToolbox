using System;
using System.Collections;
using System.Collections.Generic;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects.Components
{
    public interface ICollideBehavior
    {
        void CollideWith(IEntity collidedWith);

        /// <summary>
        ///     Called after all collisions have been processed, as well as how many collisions occured
        /// </summary>
        /// <param name="collisionCount"></param>
        void PostCollide(uint collisionCount) { }
    }

    public sealed class PhysicsComponent : Component
    {
        public override string Name => "Physics";
        public override uint? NetID => NetIDs.PHYSICS;

        // _xf is body origin transform, fucking WHAT

        /// <summary>
        ///     Swept motion for the CCD (Continuous Collision Detection).
        /// </summary>
        internal Sweep Sweep { get; set; }

        /// <summary>
        ///     Can this body rotate at all?
        /// </summary>
        private bool _fixedRotation;

        public List<Fixture> FixtureList { get; set; } = new List<Fixture>();

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
            get => _inertia + Mass * Vector2.Dot(Sweep.LocalCentre, Sweep.LocalCentre);
            set
            {
                // TODO: Debug assert
                if (BodyType != BodyType.Dynamic)
                    return;

                if (value > 0f && !_fixedRotation)
                {
                    _inertia = value - Mass * Vector2.Dot(LocalCentre, LocalCentre);
                    _invI = 1f / _inertia;
                }
            }
        }

        private float _inertia;

        /// <summary>
        ///     Inverse inertia
        /// </summary>
        private float _invI;

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
        ///     How long have we been awake for to check if we can sleep.
        /// </summary>
        internal float SleepTime { get; set; }

        /// <summary>
        ///     https://en.wikipedia.org/wiki/Torque
        /// </summary>
        public float Torque { get; set; }

        // TODO
        internal PhysicsMap Map { get; set; } = default!;

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
                    _sweep.A0 = _sweep.A;
                    _sweep.C0 = _sweep.C;
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

                    // TODO: Add to ActiveContacts in contactmanager
                    // TODO: Add to awake bodies
                }
                else
                {
                    // TODO: Remove from awakebodies
                    // TODO: ResetDynamics
                    // TODO: Remove from active contacts
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
            _invI = 0f;

            if (BodyType == BodyType.Kinematic)
            {
                // TODO: fucking WAHT
                // TODO: pretty sure this needs changing.
                throw new NotImplementedException();
                Sweep.C0 = Owner.Transform.WorldPosition;
                Sweep.C = Owner.Transform.WorldPosition;
                Sweep.A0 = _sweep.A;
                return;
            }

            DebugTools.Assert(BodyType == BodyType.Dynamic || BodyType == BodyType.Static);

            var localCentre = Vector2.Zero;
            foreach (var fixture in FixtureList)
            {
                if (fixture.Shape.Density == 0f)
                    continue;

                var massData = fixture.Shape.MassData;
                _mass += massData.Mass;
                localCentre += massData.Mass * massData.Centroid;
                _interia += massData.Inertia;
            }

            // OG comment from farseer here.
            //FPE: Static bodies only have mass, they don't have other properties. A little hacky tho...
            if (BodyType == BodyType.Static)
            {
                throw new NotImplementedException()
                _sweep.C0 = _sweep.C = _xf.p;
                return;
            }

            if (_mass > 0f)
            {
                _invMass = 1f / _mass;
                _localCentre *= _invMass;
            }
            else
            {
                // Dynamic needs a minimum mass or shit will explode
                _mass = 1f;
                _invMass = 1f;
            }

            if (_interia > 0f && !_fixedRotation)
            {
                // Centre inertia around centre of mass
                _inertia -= _mass * Vector2.Dot(localCentre, localCentre);

                DebugTools.Assert(_inertia > 0f);
                _invI = 1f / _inertia;
            }
            else
            {
                _inertia = 0f;
                _invI = 0f;
            }

            // Move centre of mass and update center of mass velocity
            var oldCentre = _sweep.C;
            _sweep.LocalCenter = localCenter;
            _sweep.C0 = _sweep.C = Transform.Multiply(ref _sweep.LocalCenter, ref _xf);

            var a = _sweep.C - oldCenter;
            _linearVelocity += new Vector2(-_angularVelocity * a.Y, _angularVelocity * a.X);
        }

        /// <summary>
         ///    Create all proxies.
         /// </summary>
        internal void CreateProxies()
        {

        }

        internal void SynchronizeFixtures()
        {
            Transform xf1 = new Transform(Vector2.Zero, _sweep.A0);
            xf1.p = _sweep.C0 - Complex.Multiply(ref _sweep.LocalCenter, ref xf1.q);

            IBroadPhase broadPhase = World.ContactManager.BroadPhase;
            for (int i = 0; i < FixtureList.Count; i++)
            {
                FixtureList[i].Synchronize(broadPhase, ref xf1, ref _xf);
            }
        }
    }

    [Serializable, NetSerializable]
    public enum BodyStatus
    {
        OnGround,
        InAir
    }

    /// <summary>
    ///     Sent whenever a <see cref="IPhysicsComponent"/> is changed.
    /// </summary>
    public sealed class PhysicsUpdateMessage : EntitySystemMessage
    {
        public IPhysicsComponent Component { get; }

        public PhysicsUpdateMessage(IPhysicsComponent component)
        {
            Component = component;
        }
    }
}
