using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.ViewVariables;

namespace Robust.Client.GameObjects
{
    /// <summary>
    ///     Contains physical properties of the entity. This component registers the entity
    ///     in the physics system as a dynamic ridged body object that has physics. This behavior overrides
    ///     the BoundingBoxComponent behavior of making the entity static.
    /// </summary>
    internal class PhysicsComponent : Component, IPhysDynamicBody
    {
        /// <inheritdoc />
        public override string Name => "Physics";

        /// <inheritdoc />
        public override uint? NetID => NetIDs.PHYSICS;

        /// <inheritdoc />
        public override Type StateType => typeof(PhysicsComponentState);
        
        public ICollidableComponent Collidable => Owner.GetComponent<ICollidableComponent>();
        public ITransformComponent Transform => Owner.GetComponent<ITransformComponent>();

        /// <summary>
        ///     Current mass of the entity in kg.
        /// </summary>
        [ViewVariables]
        public float Mass { get; set; }

        public Vector2 LinearVelocity { get; set; }
        public float AngularVelocity { get; set; }
        public bool EdgeSlide { get; set; }
        public bool Anchored { get; set; }
        public bool DidMovementCalculations { get; set; }
        public List<IPhysDynamicBody> GetVelocityConsumers()
        {
            throw new NotImplementedException();
        }

        public void AddVelocityConsumer(IPhysDynamicBody physicsComponent)
        {
            throw new NotImplementedException();
        }

        public void ClearVelocityConsumers()
        {
            throw new NotImplementedException();
        }

        public List<IPhysDynamicBody> VelocityConsumers { get; }

        /// <summary>
        ///     Current velocity of the entity.
        /// </summary>
        [ViewVariables]
        public Vector2 Velocity { get; private set; }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            if (curState == null)
                return;

            var newState = (PhysicsComponentState)curState;
            Mass = newState.Mass / 1000f; // gram to kilogram
            Velocity = newState.Velocity;
        }
    }
}
