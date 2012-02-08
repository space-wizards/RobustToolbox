using System.Collections.Generic;
using System.Linq;
using ClientInterfaces.GOC;
using GorgonLibrary;
using SS13_Shared;
using SS13_Shared.GO;

namespace CGO
{
    /// <summary>
    /// Mover component that responds to movement by an entity.
    /// </summary>
    public class SlaveMoverComponent : GameObjectComponent
    {
        IEntity master;
        Constants.MoveDirs movedir = Constants.MoveDirs.south;

        public SlaveMoverComponent()
        {
            family = SS13_Shared.GO.ComponentFamily.Mover;
        }

        public override void RecieveMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            base.RecieveMessage(sender, type, replies, list);

            switch (type)
            {
                case ComponentMessageType.SlaveAttach:
                    Attach((int)list[0]);
                    break;
            }

            return;
        }

        public override void OnRemove()
        {
            base.OnRemove();
            Detach();
        }

        private void Attach(int uid)
        {
            master = EntityManager.Singleton.GetEntity(uid);
            master.OnMove += HandleOnMove;
            Translate(master.Position);
            GetMasterMoveDirection();
        }

        private void GetMasterMoveDirection()
        {
            List<ComponentReplyMessage> replies = new List<ComponentReplyMessage>();

            master.SendMessage(this, ComponentMessageType.GetMoveDir, replies);
            if (replies.Count > 0)
            {
                movedir = (Constants.MoveDirs)replies.First().ParamsList[0];
                Owner.SendMessage(this, ComponentMessageType.MoveDirection, null, movedir);
            }
        }

        private void Detach()
        {
            if (master != null)
            {
                master.OnMove -= HandleOnMove;
                master = null;
            }
        }

        private void HandleOnMove(object sender, VectorEventArgs args)
        {
            Translate(args.Vector2D);
        }

        private void Translate(Vector2D toPosition)
        {
            Vector2D delta = toPosition - Owner.Position;

            Owner.Position = toPosition;

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
                Owner.SendMessage(this, ComponentMessageType.MoveDirection, null, movedir);
            }
        }
    }
}
