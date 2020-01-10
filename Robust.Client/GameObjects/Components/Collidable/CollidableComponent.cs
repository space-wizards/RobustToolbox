using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.ViewVariables;

namespace Robust.Client.GameObjects
{
    public class CollidableComponent : Component, ICollidableComponent
    {
#pragma warning disable 649
        [Dependency] private readonly IPhysicsManager _physicsManager;
#pragma warning restore 649

        private bool _collisionIsActuallyEnabled;
        private bool _collisionEnabled;
        private List<IPhysShape> _physShapes;

        /// <inheritdoc />
        public override string Name => "Collidable";

        /// <inheritdoc />
        public override uint? NetID => NetIDs.COLLIDABLE;

        /// <inheritdoc />
        public override Type StateType => typeof(CollidableComponentState);

        /// <inheritdoc />
        [ViewVariables]
        Box2 IPhysBody.WorldAABB
        {
            get
            {
                var pos = Owner.Transform.WorldPosition;
                return ((IPhysBody) this).AABB.Translated(pos);
            }
        }

        /// <inheritdoc />
        [ViewVariables]
        Box2 IPhysBody.AABB
        {
            get
            {
                var angle = Owner.Transform.WorldRotation;
                var bounds = new Box2();

                foreach (var shape in _physShapes)
                {
                    var shapeBounds = shape.CalculateLocalBounds(angle);
                    bounds = bounds.IsEmpty() ? shapeBounds : bounds.Union(shapeBounds);
                }

                return bounds;
            }
        }

        /// <inheritdoc />
        [ViewVariables]
        public List<IPhysShape> PhysicsShapes
        {
            get => _physShapes;
        }

        /// <inheritdoc />
        [ViewVariables]
        public MapId MapID => Owner.Transform.MapID;

        /// <inheritdoc />
        void IPhysBody.Bumped(IEntity bumpedby)
        {
            SendMessage(new BumpedEntMsg(bumpedby));
        }

        /// <inheritdoc />
        void IPhysBody.Bump(List<IEntity> bumpedinto)
        {
            var collidecomponents = Owner.GetAllComponents<ICollideBehavior>().ToList();

            for (var i = 0; i < collidecomponents.Count; i++)
            {
                collidecomponents[i].CollideWith(bumpedinto);
            }
        }

        /// <inheritdoc />
        [ViewVariables]
        public bool CollisionEnabled
        {
            get => _collisionEnabled;
            set
            {
                if (value == _collisionEnabled)
                {
                    return;
                }

                _collisionEnabled = value;
                if (value)
                {
                    EnableCollision();
                }
                else
                {
                    DisableCollision();
                }
            }
        }

        /// <inheritdoc />
        [ViewVariables]
        public bool IsHardCollidable { get; set; }

        /// <inheritdoc />
        [ViewVariables]
        public int CollisionLayer { get; set; }

        /// <inheritdoc />
        [ViewVariables]
        public int CollisionMask { get; set; }

        /// <summary>
        ///     gets the AABB from the sprite component and sends it to the CollisionManager.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            // normally ExposeData would create this
            if(_physShapes == null)
                _physShapes = new List<IPhysShape>{new PhysShapeAabb()};

            if (_collisionEnabled && !_collisionIsActuallyEnabled)
            {
                _physicsManager.AddBody(this);
                _collisionIsActuallyEnabled = true;
            }
        }

        /// <summary>
        ///     removes the AABB from the CollisionManager.
        /// </summary>
        protected override void Shutdown()
        {
            if (_collisionEnabled)
            {
                _physicsManager.RemoveBody(this);
            }

            base.Shutdown();
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            if (curState == null)
                return;

            var newState = (CollidableComponentState) curState;

            // edge triggered
            if (newState.CollisionEnabled != _collisionEnabled)
            {
                if (newState.CollisionEnabled)
                    EnableCollision();
                else
                    DisableCollision();
            }

            if (newState.PhysShapes != null)
            {
                _physShapes = newState.PhysShapes;

                foreach (var shape in _physShapes)
                {
                    shape.ApplyState();
                }
            }


        }

        /// <inheritdoc />
        public bool TryCollision(Vector2 offset, bool bump = false)
        {
            return _physicsManager.TryCollide(Owner, offset, bump);
        }

        /// <summary>
        ///     Enables PhysicsBody
        /// </summary>
        private void EnableCollision()
        {
            _collisionEnabled = true;
            _collisionIsActuallyEnabled = true;
            _physicsManager.AddBody(this);
        }

        /// <summary>
        ///     Disables PhysicsBody
        /// </summary>
        private void DisableCollision()
        {
            _collisionEnabled = false;
            _collisionIsActuallyEnabled = false;
            _physicsManager.RemoveBody(this);
        }
    }
}
