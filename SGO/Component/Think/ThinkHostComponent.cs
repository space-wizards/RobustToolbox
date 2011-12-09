using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SGO
{
    public class ThinkHostComponent : GameObjectComponent
    {
        public ThinkHostComponent()
        {
            family = SS3D_shared.GO.ComponentFamily.Think;
        }

        public override void RecieveMessage(object sender, SS3D_shared.GO.ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            base.RecieveMessage(sender, type, replies, list);
            switch(type)
            {
                case SS3D_shared.GO.ComponentMessageType.Bumped:
                    OnBump(sender, list);
                    break;
            }
        }

        public void OnBump(object sender, params object[] list)
        {
            ServerServices.LogManager.Log("Bumped!");
        }

    }
}
