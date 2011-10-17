using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.GO;
using ServerServices;
using ServerInterfaces;

namespace SGO
{
    public class DamageableComponent : GameObjectComponent
    {
        public int maxHealth = 100;
        public int currentHealth = 100;

        public DamageableComponent()
        {
            family = SS3D_shared.GO.ComponentFamily.Damageable;
        }

        public override void RecieveMessage(object sender, SS3D_shared.GO.ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            switch (type)
            {
                case SS3D_shared.GO.ComponentMessageType.Damage:
                    /// Who damaged, how much, what type
                    ApplyDamage((Entity)list[0], (int)list[1], (DamageType)list[2]);
                    break;

            }
        }

        private void ApplyDamage(Entity damager, int damageamount, DamageType damType)
        {
            int damagetoapply = damageamount - GetArmorValues();
            currentHealth -= damagetoapply;
            if (currentHealth <= 0)
            {
                Owner.SendMessage(this, ComponentMessageType.Die, null);
                IChatManager cm = (IChatManager)ServiceManager.Singleton.GetService(ServerServiceType.ChatManager);
                cm.SendChatMessage(ChatChannel.Default, Owner.name + " has died.", null, Owner.Uid);

            }
        }

        private int GetArmorValues()
        {
            //TODO do armor by damagetype
            int armorvalues = 0;
            List<ComponentReplyMessage> replies = new List<ComponentReplyMessage>();
            Owner.SendMessage(this, ComponentMessageType.GetArmorValues, replies);
            foreach (ComponentReplyMessage reply in replies)
            {
                if (reply.messageType == ComponentMessageType.ReturnArmorValues && reply.paramsList[0].GetType() == typeof(int))
                {
                    armorvalues += (int)reply.paramsList[0];
                }
            }
            return armorvalues;
        }

        private void ApplyDamage(int p)
        {
            throw new NotImplementedException();
        }

    }
}
