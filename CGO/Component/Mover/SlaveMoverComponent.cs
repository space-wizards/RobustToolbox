using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GorgonLibrary;
using SS3D_shared;

namespace CGO
{
    /// <summary>
    /// Mover component that responds to movement by an entity.
    /// </summary>
    public class SlaveMoverComponent : GameObjectComponent
    {
        Entity master;
        Constants.MoveDirs movedir = Constants.MoveDirs.south;

        public SlaveMoverComponent()
        {
            family = SS3D_shared.GO.ComponentFamily.Mover;
        }

        public override void RecieveMessage(object sender, MessageType type, params object[] list)
        {
            switch (type)
            {
                case MessageType.SlaveAttach:
                    Attach((int)list[0]);
                    break;
            }

        }

        public override void OnRemove()
        {
            base.OnRemove();
            Detach();
        }

        private void Attach(int uid)
        {
            master = EntityManager.Singleton.GetEntity(uid);
            master.OnMove += new Entity.EntityMoveEvent(HandleOnMove);
            Translate(master.position);
        }

        private void Detach()
        {
            if (master != null)
            {
                master.OnMove -= new Entity.EntityMoveEvent(HandleOnMove);
                master = null;
            }
        }

        private void HandleOnMove(Vector2D toPosition)
        {
            Translate(toPosition);
        }

        private void Translate(Vector2D toPosition)
        {
            Vector2D delta = toPosition - Owner.position;

            Owner.position = toPosition;

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
                Owner.SendMessage(this, MessageType.MoveDirection, movedir);
            }
        }
    }
}
