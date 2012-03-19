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
        private IEntity _master;
        private Constants.MoveDirs _movedir = Constants.MoveDirs.south;

        public override ComponentFamily Family
        {
            get { return ComponentFamily.Mover; }
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type, params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

            if (sender == this) //Don't listen to our own messages!
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.SlaveAttach:
                    Attach((int)list[0]);
                    break;
            }

            return reply;
        }

        public override void OnRemove()
        {
            base.OnRemove();
            Detach();
        }

        private void Attach(int uid)
        {
            _master = EntityManager.Singleton.GetEntity(uid);
            _master.OnMove += HandleOnMove;
            Translate(_master.Position);
            GetMasterMoveDirection();
        }

        private void GetMasterMoveDirection()
        {
            var reply = _master.SendMessage(this, ComponentFamily.Mover, ComponentMessageType.GetMoveDir);

            if (reply.MessageType == ComponentMessageType.MoveDirection)
            {
                _movedir = (Constants.MoveDirs)reply.ParamsList[0];
                Owner.SendMessage(this, ComponentMessageType.MoveDirection, _movedir);
            }
        }

        private void Detach()
        {
            if (_master == null) return;

            _master.OnMove -= HandleOnMove;
            _master = null;
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

        private void SetMoveDir(Constants.MoveDirs movedir)
        {
            if (movedir == _movedir) return;

            _movedir = movedir;
            Owner.SendMessage(this, ComponentMessageType.MoveDirection, _movedir);
        }
    }
}
