using GameObject;
using SS13_Shared;
using SS13_Shared.GO;

namespace SGO
{
    /// <summary>
    /// Mover component that responds to movement by an entity.
    /// </summary>
    public class SlaveMoverComponent : Component
    {
        private Entity master;

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
            master = Owner.EntityManager.GetEntity(uid);
            master.OnShutdown += master_OnShutdown;
            master.GetComponent<TransformComponent>(ComponentFamily.Transform).OnMove += HandleOnMasterMove;
            Translate(master.GetComponent<TransformComponent>(ComponentFamily.Transform).Position);
        }

        private void master_OnShutdown(Entity e)
        {
            Detach();
        }

        private void Detach()
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

        public void Translate(Vector2 toPosition)
        {
            Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position = toPosition;
        }
    }
}