using System;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Maths;
using SS14.Shared.Serialization;
using SS14.Shared.ViewVariables;

namespace SS14.Shared.GameObjects.Components.BoundingBox
{
    /// <summary>
    ///     Holds an Axis Aligned Bounding Box (AABB) for the entity. Using this component adds the entity
    ///     to the physics system as a static (non-movable) entity.
    /// </summary>
    public class BoundingBoxComponent : Component
    {
        private Box2 _aabb;


        /// <inheritdoc />
        public sealed override string Name => "BoundingBox";

        /// <inheritdoc />
        public sealed override uint? NetID => NetIDs.BOUNDING_BOX;

        /// <inheritdoc />
        public sealed override Type StateType => typeof(BoundingBoxComponentState);

        /// <summary>
        ///     Local Axis Aligned Bounding Box of the entity.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public Box2 AABB
        {
            get => _aabb;
            set
            {
                _aabb = value;
                Dirty();
            }
        }

        /// <summary>
        ///     World Axis Aligned Bounding Box of the entity.
        /// </summary>
        [ViewVariables]
        public Box2 WorldAABB
        {
            get
            {
                var trans = Owner.GetComponent<ITransformComponent>();
                return AABB.Translated(trans.WorldPosition);
            }
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            return new BoundingBoxComponentState(_aabb);
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState state)
        {
            AABB = ((BoundingBoxComponentState)state).AABB;
        }

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _aabb, "aabb", new Box2(-0.5f, -0.5f, 0.5f, 0.5f));
        }

        /// <summary>
        ///     Serialized state of a BoundingBoxComponent.
        /// </summary>
        [Serializable, NetSerializable]
        protected class BoundingBoxComponentState : ComponentState
        {
            /// <summary>
            ///     Current AABB of the entity.
            /// </summary>
            public readonly Box2 AABB;

            /// <summary>
            ///     Constructs a new state snapshot of a PhysicsComponent.
            /// </summary>
            /// <param name="aabb">Current AABB of the entity.</param>
            public BoundingBoxComponentState(Box2 aabb)
                : base(NetIDs.BOUNDING_BOX)
            {
                AABB = aabb;
            }
        }
    }
}
