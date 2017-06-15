using SFML.System;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.Mover;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;

namespace SS14.Server.GameObjects
{
    /// <summary>
    /// Mover component that responds to movement by an entity.
    /// </summary>
    [IoCTarget]
    public class SlaveMoverComponent : Component
    {
        public override string Name => "SlaveMover";
        private IEntity master;

        public SlaveMoverComponent()
        {
            Family = ComponentFamily.Mover;
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

            if (sender == this)
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

        public void Attach(int uid)
        {
            master = Owner.EntityManager.GetEntity(uid);
            master.OnShutdown += master_OnShutdown;
            master.GetComponent<TransformComponent>(ComponentFamily.Transform).OnMove += HandleOnMasterMove;
            Translate(master.GetComponent<TransformComponent>(ComponentFamily.Transform).Position);
        }

        public void Attach(IEntity newMaster)
        {
            master = newMaster;
            master.OnShutdown += master_OnShutdown;
            master.GetComponent<TransformComponent>(ComponentFamily.Transform).OnMove += HandleOnMasterMove;
            Translate(master.GetComponent<TransformComponent>(ComponentFamily.Transform).Position);
        }

        private void master_OnShutdown(IEntity e)
        {
            Detach();
        }

        public void Detach()
        {
            if (master != null)
            {
                master.GetComponent<TransformComponent>(ComponentFamily.Transform).OnMove -= HandleOnMasterMove;
                master = null;
            }
        }

        private void HandleOnMasterMove(object sender, VectorEventArgs args)
        {
            Translate(args.VectorTo);
        }

        public void Translate(Vector2f toPosition)
        {
            Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position = toPosition;
        }

        private TransformComponent getTransform()
        {
            return Owner.GetComponent<TransformComponent>(ComponentFamily.Transform);
        }

        public override ComponentState GetComponentState()
        {
            var transform = getTransform();
            if (master == null)
            {
                return new MoverComponentState(transform.X, transform.Y, 0, 0);
            }
            return new MoverComponentState(transform.X, transform.Y, 0, 0, master.Uid);
        }
    }
}
