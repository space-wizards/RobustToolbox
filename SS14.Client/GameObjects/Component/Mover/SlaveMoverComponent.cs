using SFML.System;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.Mover;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using System;

namespace SS14.Client.GameObjects
{
    /// <summary>
    /// Mover component that responds to movement by an entity.
    /// </summary>
    [IoCTarget]
    public class SlaveMoverComponent : ClientComponent
    {
        public override string Name => "SlaveMover";
        private IEntity _master;

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
            Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position = toPosition;
        }

        public override void HandleComponentState(dynamic state)
        {
            SetNewState(state);
        }

        private void SetNewState(MoverComponentState state)
        {
            if (_master == null && state.Master != null)
            {
                Attach((int)state.Master);
            }
            if (_master != null && state.Master == null)
            {
                Detach();
            }
            if (_master != null && state.Master != null && _master.Uid != state.Master)
            {
                Detach();
                Attach((int)state.Master);
            }
        }
    }
}
