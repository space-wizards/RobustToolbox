using System;
using ClientInterfaces.Configuration;
using GameObject;
using GorgonLibrary;
using SS13.IoC;
using SS13_Shared;
using SS13_Shared.GO;
using SS13_Shared.GO.Component.Mover;
using SS13_Shared.GO.Component.Transform;

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

        public override Type StateType
        {
            get { return typeof(MoverComponentState); }
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

        public override void HandleComponentState(dynamic state)
        {
            SetNewState(state);
        }

        private void SetNewState(MoverComponentState state)
        {
            if(_master == null && state.Master != null)
            {
                Attach((int)state.Master);
            }
            if(_master != null && state.Master == null)
            {
                Detach();
            }
            if(_master != null && state.Master != null && _master.Uid != state.Master)
            {
                Detach();
                Attach((int)state.Master);
            }
        }
    }
}