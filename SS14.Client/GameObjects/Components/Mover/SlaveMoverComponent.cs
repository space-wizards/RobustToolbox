using SFML.System;
using SS14.Client.Interfaces.GameObjects;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components;
using SS14.Shared.GameObjects.Components.Mover;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using System;

namespace SS14.Client.GameObjects
{
    /// <summary>
    /// Mover component that responds to movement by an entity.
    /// </summary>
    public class SlaveMoverComponent : ClientComponent, IMoverComponent
    {
        public override string Name => "SlaveMover";
        public override uint? NetID => NetIDs.SLAVE_MOVER;
        public override bool NetworkSynchronizeExistence => true;
        private IEntity _master;

        public override Type StateType => typeof(SlaveMoverComponentState);

        public override void OnRemove()
        {
            Detach();
            base.OnRemove();
        }

        private void Attach(int uid)
        {
            _master = Owner.EntityManager.GetEntity(uid);
            // TODO handle this using event queue so that these sorts of interactions are deferred until we can be sure the target entity exists
            _master.GetComponent<ITransformComponent>().OnMove += HandleOnMove;
            Translate(_master.GetComponent<ITransformComponent>().Position);
        }

        private void Detach()
        {
            if (_master == null) return;

            _master.GetComponent<ITransformComponent>().OnMove -= HandleOnMove;
            _master = null;
        }

        private void HandleOnMove(object sender, VectorEventArgs args)
        {
            Translate(args.VectorTo);
        }

        private void Translate(Vector2f toPosition)
        {
            Owner.GetComponent<ITransformComponent>().Position = toPosition;
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState state)
        {
            var newState = (SlaveMoverComponentState) state;

            if (_master == null && newState.Master != null)
                Attach((int) newState.Master);

            if (_master != null && newState.Master == null)
                Detach();

            if (_master == null || newState.Master == null || _master.Uid == newState.Master)
                return;

            Detach();
            Attach((int) newState.Master);
        }
    }
}
