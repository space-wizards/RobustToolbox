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
            set
            {
            }
        }
        
        public virtual void RecieveMessage(object sender, CGO.MessageType type, params object[] list)
        {
            if (sender == this) //Don't listen to our own messages!
                return;
        }

        public virtual void Shutdown()
        {

        }

        public virtual void OnRemove()
        {
            Owner = null;
            Shutdown();
            //Send us to the manager so it knows we're dead.
            ComponentManager.Singleton.RemoveComponent(this);
        }

        public virtual void OnAdd(Entity owner)
        {
            Owner = owner;
            //Send us to the manager so it knows we're active
            ComponentManager.Singleton.AddComponent(this);
        }

        public virtual void Update(float frameTime)
        {

        }
    }
}
