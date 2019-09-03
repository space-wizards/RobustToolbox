using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Server.GameObjects
{
    /// <summary>
    ///     Contains physical properties of the entity. This component registers the entity
    ///     in the physics system as a dynamic ridged body object that has physics. This behavior overrides
    ///     the BoundingBoxComponent behavior of making the entity static.
    /// </summary>
    public class PhysicsComponent : Component, Shared.Interfaces.GameObjects.Components.ICollideSpecial
    {
        private float _mass;
        private Vector2 _linVelocity;
        private float _angVelocity;

        /// <inheritdoc />
        public override string Name => "Physics";

        /// <inheritdoc />
        public override uint? NetID => NetIDs.PHYSICS;

        /// <summary>
        ///     Current mass of the entity in kilograms.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float Mass
        {
            get => _mass;
            set
            {
                _mass = value;
                Dirty();
            }
        }

        /// <summary>
        ///     Current linear velocity of the entity in meters per second.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 LinearVelocity
        {
            get => _linVelocity;
            set
            {
                if(_linVelocity == value)
                    return;

                _linVelocity = value;
                Dirty();
            }
        }

        /// <summary>
        ///     Current angular velocity of the entity in radians per sec.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float AngularVelocity
        {
            get => _angVelocity;
            set
            {
                if(_angVelocity.Equals(value))
                    return;

                _angVelocity = value;
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool EdgeSlide { get => edgeSlide; set => edgeSlide = value; }
        private bool edgeSlide = true;

        [ViewVariables(VVAccess.ReadWrite)]
        private bool _anchored;
        public bool Anchored
        {
            get => _anchored;
            set
            {
                _anchored = value;
            }
        }

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _mass, "mass", 1);
            serializer.DataField(ref _linVelocity, "vel", Vector2.Zero);
            serializer.DataField(ref _angVelocity, "avel", 0.0f);
            serializer.DataField(ref edgeSlide, "edgeslide", true);
            serializer.DataField(ref _anchored, "Anchored", true);
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            return new PhysicsComponentState(_mass, _linVelocity);
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);

            switch (message)
            {
                case BumpedEntMsg msg:
                    if (Anchored)
                    {
                        return;
                    }

                    if (!msg.Entity.TryGetComponent(out PhysicsComponent physicsComponent))
                    {
                        return;
                    }
                    physicsComponent.AddVelocityConsumer(this);
                    break;
            }
        }

        private List<PhysicsComponent> VelocityConsumers { get; } = new List<PhysicsComponent>();

        public List<PhysicsComponent> GetVelocityConsumers()
        {
            var result = new List<PhysicsComponent> { this };
            foreach(var velocityConsumer in VelocityConsumers)
            {
                result.AddRange(velocityConsumer.GetVelocityConsumers());
            }
            return result;
        }

        private void AddVelocityConsumer(PhysicsComponent physicsComponent)
        {
            if (!physicsComponent.VelocityConsumers.Contains(this) && !VelocityConsumers.Contains(physicsComponent))
            {
                VelocityConsumers.Add(physicsComponent);
            }
        }

        internal void ClearVelocityConsumers()
        {
            VelocityConsumers.ForEach(x => x.ClearVelocityConsumers());
            VelocityConsumers.Clear();
        }

        public bool PreventCollide(IPhysBody collidedwith)
        {
            var velocityConsumers = GetVelocityConsumers();
            if (velocityConsumers.Count == 1 || !collidedwith.Owner.TryGetComponent<PhysicsComponent>(out var physicsComponent))
            {
                return false;
            }
            return velocityConsumers.Contains(physicsComponent);
        }

        public bool DidMovementCalculations { get; set; } = false;
    }
}
