using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GorgonLibrary;
using SS3D_shared;

namespace CGO
{
    /// <summary>
    /// Recieves movement data from the server and updates the entity's position accordingly.
    /// </summary>
    public class NetworkMoverComponent : GameObjectComponent
    {
        private Constants.MoveDirs movedir = Constants.MoveDirs.south;

        public NetworkMoverComponent()
        {
            family = SS3D_shared.GO.ComponentFamily.Mover;
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {
            double x = (double)message.messageParameters[0];
            double y = (double)message.messageParameters[1];
            Translate((float)x, (float)y);
        }

        private void Translate(float x, float y)
        {
            Vector2D delta = new Vector2D(x, y) - Owner.position;

            Owner.position.X = x;
            Owner.position.Y = y;
            
            if (delta.X > 0 && delta.Y > 0)
                SetMoveDir(Constants.MoveDirs.southeast);
            if (delta.X > 0 && delta.Y < 0)
                SetMoveDir(Constants.MoveDirs.northeast);
            if (delta.X < 0 && delta.Y > 0)
                SetMoveDir(Constants.MoveDirs.southwest);
            if (delta.X < 0 && delta.Y < 0)
                SetMoveDir(Constants.MoveDirs.northwest);
            if (delta.X > 0 && delta.Y == 0)
                SetMoveDir(Constants.MoveDirs.east);
            if (delta.X < 0 && delta.Y == 0)
                SetMoveDir(Constants.MoveDirs.west);
            if (delta.Y > 0 && delta.X == 0)
                SetMoveDir(Constants.MoveDirs.south);
            if (delta.Y < 0 && delta.X == 0)
                SetMoveDir(Constants.MoveDirs.north);

            Owner.Moved();
        }

        private void SetMoveDir(Constants.MoveDirs _movedir)
        {
            if (_movedir != movedir)
            {
                movedir = _movedir;
                Owner.SendMessage(this, MessageType.MoveDirection, null, movedir);
            }
        }
    }
}
