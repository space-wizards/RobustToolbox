using GameObject;
using GorgonLibrary;
using SS13_Shared;
using SS13_Shared.GO;

namespace CGO
{
    /// <summary>
    /// Mover component that responds to movement by an entity.
    /// </summary>
    public class SlaveMoverComponent : Component
    {
        private Entity _master;
        private Direction _movedir = Direction.South;

        public SlaveMoverComponent()
        {
            Family = ComponentFamily.Mover;
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

            if (sender == this) //Don't listen to our own messages!
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.SlaveAttach:
                    Attach((int) list[0]);
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
            _master = Owner.EntityManager.GetEntity(uid);
            _master.GetComponent<TransformComponent>(ComponentFamily.Transform).OnMove += HandleOnMove;
            Translate(_master.GetComponent<TransformComponent>(ComponentFamily.Transform).Position);
        }

        private void Detach()
        {
            if (_master == null) return;

            _master.GetComponent<TransformComponent>(ComponentFamily.Transform).OnMove -= HandleOnMove;
            _master = null;
        }

        private void HandleOnMove(object sender, VectorEventArgs args)
        {
            Translate(Vector2TypeConverter.ToVector2D(args.VectorTo));
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
    }
}