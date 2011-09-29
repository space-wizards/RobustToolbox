using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.HelperClasses;

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
            family = SS3D_shared.GO.ComponentFamily.Mover;
        }

        public override ComponentReplyMessage RecieveMessage(object sender, MessageType type, params object[] list)
        {
            switch (type)
            {
                case MessageType.SlaveAttach:
                    Attach((int)list[0]);
                    break;
            }
            return ComponentReplyMessage.Null;
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

        private void HandleOnMove(Vector2 toPosition)
        {
            Translate(toPosition);
        }

        public void Translate(Vector2 toPosition)
        {
            Owner.position = toPosition;
            Owner.Moved();
        }
    }
}
