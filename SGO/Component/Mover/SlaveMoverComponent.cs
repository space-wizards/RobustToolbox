using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared.HelperClasses;
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

        public override void RecieveMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            switch (type)
            {
                case ComponentMessageType.SlaveAttach:
                    Attach((int)list[0]);
                    break;
            }
            return;
        }

        public override void OnRemove()
        {
            base.OnRemove();
            Detach();
        }

        private void Attach(int uid)
        {
            master = EntityManager.Singleton.GetEntity(uid);
            master.OnMove += new Entity.EntityMoveEvent(HandleOnMove);
            Translate(master.position);
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
