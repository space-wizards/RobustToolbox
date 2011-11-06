using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.GO;
using ServerServices;
using ServerInterfaces;
using Organs = SGO.Component.Health.Organs;

namespace SGO
{
    public class HealthComponent : DamageableComponent
    {
        public Organs.BLOOD_TYPE blood_type = Organs.BLOOD_TYPE.A; // Temporary
        public List<Organs.Organ> organs = new List<Organs.Organ>();
        private float lastHealth = 100f;
        private float healthSendTime = 500;
        private float healthSendCounter = 0;
        private bool isDead = false;

        public HealthComponent()
            : base()
        {
        }

        public override void  Update(float frameTime)
        {
 	        base.Update(frameTime);
            foreach (Organs.Organ organ in organs)
            {
                organ.Process(frameTime);
            }

            

            healthSendCounter += frameTime;
            if (healthSendCounter > healthSendTime)
            {
                CheckDeath();
                if (GetHealth() != lastHealth)
                {
                    lastHealth = GetHealth();
                    SendHealthUpdate();
                }
                healthSendCounter = 0;
            }

        }

        public override void RecieveMessage(object sender, SS3D_shared.GO.ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            switch (type)
            {
                case SS3D_shared.GO.ComponentMessageType.Damage:
                    // Who damaged, how much, what type
                    ApplyDamage((Entity)list[0], (int)list[1], (DamageType)list[2]);
                    break;
            }
        }

        private void ApplyDamage(Entity damager, int damageamount, DamageType damType)
        {
            foreach (Organs.Organ o in organs)
            {

                o.Damage(damager, damageamount / organs.Count, damType);

            }

            SendHealthUpdate();
        }

        public float GetHealth()
        {
            float health = 0;
            foreach (Organs.Organ o in organs)
            {
                health += o.blood.amount;
            }
            return health;
        }

        private void SendHealthUpdate()
        {
            float health = GetHealth();
            Owner.SendComponentNetworkMessage(this, Lidgren.Network.NetDeliveryMethod.ReliableOrdered, null, ComponentMessageType.HealthStatus, health);
        }

        public bool IsDead()
        {
            return isDead;
        }

        private void CheckDeath()
        {
            if (isDead)
                return;

            if (GetHealth() <= 0)
            {
                isDead = true;
                Owner.SendMessage(this, ComponentMessageType.Die, null);
                IChatManager cm = (IChatManager)ServiceManager.Singleton.GetService(ServerServiceType.ChatManager);
                cm.SendChatMessage(ChatChannel.Default, Owner.name + " has died.", null, Owner.Uid);
            }
        }
    }
}
