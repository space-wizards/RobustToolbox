using System;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Physics;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;

namespace SS14.Server.GameObjects
{
    public class CollidableComponent : Component, ICollidableComponent
    {
        private readonly bool _collisionEnabled = true;

        public event EventHandler<BumpEventArgs> OnBump;

        /// <inheritdoc />
        public override string Name => "Collidable";

        /// <inheritdoc />
        public override uint? NetID => NetIDs.COLLIDABLE;

        /// <inheritdoc />
        public MapId MapID => Owner.GetComponent<ITransformComponent>().MapID;

        /// <inheritdoc />
        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {
            switch ((ComponentMessageType)message.MessageParameters[0])
            {
                case ComponentMessageType.Bumped:
                    //TODO check who bumped us, how far away they are, etc.
                    var bumper = Owner.EntityManager.GetEntity(new EntityUid((int)message.MessageParameters[1]));
                    if (bumper != null)
                        Owner.SendMessage(this, ComponentMessageType.Bumped, bumper);
                    break;
            }
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            return new CollidableComponentState(_collisionEnabled);
        }

        /// <inheritdoc />
        Box2 ICollidable.WorldAABB => Owner.GetComponent<BoundingBoxComponent>().WorldAABB;

        /// <inheritdoc />
        Box2 ICollidable.AABB => Owner.GetComponent<BoundingBoxComponent>().AABB;

        /// <inheritdoc />
        public bool IsHardCollidable { get; set; } = true;

        /// <inheritdoc />
        void ICollidable.Bump(IEntity ent)
        {
            OnBump?.Invoke(this, new BumpEventArgs(Owner, ent));
        }

        /// <summary>
        ///     Gets the AABB from the sprite component and sends it to the collision manager.
        /// </summary>
        /// <param name="owner"></param>
        public override void OnAdd(IEntity owner)
        {
            base.OnAdd(owner);
            var cm = IoCManager.Resolve<ICollisionManager>();
            cm.AddCollidable(this);
        }

        /// <summary>
        ///     Removes the AABB from the collisionmanager.
        /// </summary>
        public override void Shutdown()
        {
            var cm = IoCManager.Resolve<ICollisionManager>();
            cm.RemoveCollidable(this);
            base.Shutdown();
        }

        public bool TryCollision(Vector2 offset, bool bump = false)
        {
            return IoCManager.Resolve<ICollisionManager>().TryCollide(Owner, offset, bump);
        }
    }
}
