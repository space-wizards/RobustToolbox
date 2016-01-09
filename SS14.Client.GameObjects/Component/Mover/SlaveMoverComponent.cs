using SFML.System;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.GO.Component.Mover;
using System;

namespace SS14.Client.GameObjects
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
            // TODO handle this using event queue so that these sorts of interactions are deferred until we can be sure the target entity exists
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
            Translate(args.VectorTo);
        }

        private void Translate(Vector2f toPosition)
        {
            Vector2f delta = toPosition - Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position;

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