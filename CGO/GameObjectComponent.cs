using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CGO
{
    public class GameObjectComponent : IGameObjectComponent
    {
        private Entity m_owner;
        public Entity Owner
        {
            get { return m_owner; }
            set { m_owner = value; }
        }

        protected ComponentFamily family = ComponentFamily.Generic;
        public ComponentFamily Family
        {
            get
            {
                return family;
            }
            private set
            {
            }
        }

        public virtual void RecieveMessage(CGO.MessageType type, params object[] list)
        {

        }

        public virtual void OnRemove()
        {
            Owner = null;
        }

        public virtual void OnAdd(Entity owner)
        {
            Owner = owner;
        }

        public virtual void Update(float frameTime)
        {

        }
    }
}
