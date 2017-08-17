using System;
using Lidgren.Network;
using OpenTK;
using SFML.Graphics;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;

namespace SS14.Server.GameObjects
{
    public class CollidableComponent : Component, ICollidableComponent
    {
        private readonly bool _collisionEnabled = true;
        public override string Name => "Collidable";
        public override uint? NetID => NetIDs.COLLIDABLE;

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection client)
        {
            switch ((ComponentMessageType) message.MessageParameters[0])
            {
                case ComponentMessageType.Bumped:
                    ///TODO check who bumped us, how far away they are, etc.
                    var bumper = Owner.EntityManager.GetEntity((int) message.MessageParameters[1]);
                    if (bumper != null)
                        Owner.SendMessage(this, ComponentMessageType.Bumped, bumper);
                    break;
            }
        }

        public override ComponentState GetComponentState()
        {
            return new CollidableComponentState(_collisionEnabled);
        }

        public Box2 WorldAABB { get; }
        public Box2 AABB { get; }
        public bool IsHardCollidable { get; }

        public void Bump(IEntity ent)
        {
            throw new NotImplementedException();
        }
    }
}
