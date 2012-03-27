using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared;
using SS13_Shared.GO;

namespace SGO
{
    /// <summary>
    /// Mover component that responds to movement by an entity.
    /// </summary>
    public class SlaveMoverComponent : GameObjectComponent
    {
        Entity master;
        public SlaveMoverComponent()
        {
            family = SS13_Shared.GO.ComponentFamily.Mover;
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type, params object[] list)
        {
            var reply = base.RecieveMessage(sender, type, list);

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

        private void Attach(int uid)
        {
            master = EntityManager.Singleton.GetEntity(uid);
            master.OnShutdown += new Entity.ShutdownEvent(master_OnShutdown);
            master.OnMove += new Entity.EntityMoveEvent(HandleOnMove);
            Translate(master.position);
        }

        void master_OnShutdown(Entity e)
        {
            Detach();
        }

        private void Detach()
        {
            if (master != null)
            {
                master.OnMove -= new Entity.EntityMoveEvent(HandleOnMove);
                master = null;
            }
        }

        private void HandleOnMove(Vector2 toPosition, Vector2 fromPosition)
        {
            Translate(toPosition);
        }

        public void Translate(Vector2 toPosition)
        {
            Vector2 oldPosition = Owner.position;
            Owner.position = toPosition;
            Owner.Moved(oldPosition);
        }
    }
}
