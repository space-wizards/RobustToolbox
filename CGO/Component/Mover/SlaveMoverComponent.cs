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
        private Direction _movedir = Direction.South;
        
        public SlaveMoverComponent():base()
        {
            Family = ComponentFamily.Mover;
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
            Detach();
            base.OnRemove();
        }

        private void Attach(int uid)
        {
            _master = EntityManager.Singleton.GetEntity(uid);
            _master.GetComponent<TransformComponent>(ComponentFamily.Transform).OnMove += HandleOnMove;
            Translate(_master.GetComponent<TransformComponent>(ComponentFamily.Transform).Position);
            GetMasterMoveDirection();
        }

        private void GetMasterMoveDirection()
        {
            var reply = _master.SendMessage(this, ComponentFamily.Mover, ComponentMessageType.GetMoveDir);

            if (reply.MessageType == ComponentMessageType.MoveDirection)
            {
                SetMoveDir((Direction)reply.ParamsList[0]);
                Owner.SendMessage(this, ComponentMessageType.MoveDirection, _movedir);
            }
        }

        private void Detach()
        {
            if (_master == null) return;

            _master.GetComponent<TransformComponent>(ComponentFamily.Transform).OnMove -= HandleOnMove;
            _master = null;
        }

        private void HandleOnMove(object sender, VectorEventArgs args)
        {
            Translate(args.VectorTo);
            GetMasterMoveDirection();
        }

        private void Translate(Vector2D toPosition)
        {
            Vector2D delta = toPosition - Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position;

            Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position = toPosition;
            /*
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
             */

            //Owner.Moved();
        }

        private void SetMoveDir(Direction movedir)
        {
            if (movedir == _movedir) return;

            _movedir = movedir;
            Owner.SendMessage(this, ComponentMessageType.MoveDirection, _movedir);
        }
    }
}
