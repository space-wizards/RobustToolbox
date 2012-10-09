using System;
using System.Collections.Generic;
using System.Linq;
using Lidgren.Network;
using SS13.IoC;
using SS13_Shared;
using SS13_Shared.GO;
using ServerInterfaces.GameObject;
using ServerInterfaces.Map;
using ServerInterfaces.Player;

namespace SGO
{
    public class HumanHealthComponent : HealthComponent
    {
        protected List<DamageLocation> damageZones = new List<DamageLocation>();
        private DateTime _lastUpdate;

        public HumanHealthComponent()
        {
            damageZones.Add(new DamageLocation(BodyPart.Left_Arm, 50));
            damageZones.Add(new DamageLocation(BodyPart.Right_Arm, 50));
            damageZones.Add(new DamageLocation(BodyPart.Groin, 50));
            damageZones.Add(new DamageLocation(BodyPart.Head, 50));
            damageZones.Add(new DamageLocation(BodyPart.Left_Leg, 50));
            damageZones.Add(new DamageLocation(BodyPart.Right_Leg, 50));
            damageZones.Add(new DamageLocation(BodyPart.Torso, 100));

            maxHealth = damageZones.Sum(x => x.maxHealth);
            currentHealth = maxHealth;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            if ((DateTime.Now - _lastUpdate).TotalSeconds < 1)
                return;
            _lastUpdate = DateTime.Now;
            var map = IoCManager.Resolve<IMapManager>();

            if(map.GetGasAmount(map.GetTileArrayPositionFromWorldPosition(Owner.Position), GasType.Toxin) > 10)
            {
                //ApplyDamage(null, 3, DamageType.Toxin, BodyPart.Torso);
                var statuscomp = (StatusEffectComp)Owner.GetComponent(ComponentFamily.StatusEffects);
                statuscomp.AddEffect("ToxinInhalation", 20);
            }
        }

        public override void HandleInstantiationMessage(NetConnection netConnection)
        {
            SendHealthUpdate(netConnection);
        }

        protected void ApplyDamage(IEntity damager, int damageamount, DamageType damType, BodyPart targetLocation)
        {
            DamagedBy(damager, damageamount, damType);

            int actualDamage = Math.Max(damageamount - GetArmor(damType), 0);

            if (GetHealth() - actualDamage < 0) //No negative total health.
                actualDamage = (int) GetHealth();

            if (damageZones.Exists(x => x.location == targetLocation))
            {
                DamageLocation dmgLoc = damageZones.First(x => x.location == targetLocation);
                dmgLoc.AddDamage(damType, actualDamage);
            }

            if(targetLocation == BodyPart.Head && actualDamage > 5)
            {
                var r = Owner.SendMessage(this, ComponentFamily.Actor, ComponentMessageType.GetActorSession);
                if(r.MessageType == ComponentMessageType.ReturnActorSession)
                {
                    var s = (IPlayerSession) r.ParamsList[0];
                    s.AddPostProcessingEffect(PostProcessingEffectType.Blur, 5);
                }
            }

            TriggerBleeding(damageamount, damType, targetLocation);

            currentHealth = GetHealth();
            maxHealth = GetMaxHealth();

            SendHealthUpdate();

            if(GetHealth() <= 0)
            {
                Die();
            }
        }

        private void ApplyHeal(Entity healer, int healAmount, DamageType damType, BodyPart targetLocation)
        {
            if (isDead)
                return;
            float realHealAmount = healAmount;

            if (GetHealth() + healAmount > GetMaxHealth())
                realHealAmount = GetMaxHealth() - GetHealth();

            if (damageZones.Exists(x => x.location == targetLocation))
            {
                DamageLocation dmgLoc = damageZones.First(x => x.location == targetLocation);
                dmgLoc.HealDamage(damType, (int)realHealAmount);
            }

            currentHealth = GetHealth();
            maxHealth = GetMaxHealth();

            if(damType == DamageType.Slashing)
            {
                var statuscomp = (StatusEffectComp)Owner.GetComponent(ComponentFamily.StatusEffects);
                if(statuscomp.HasEffect("Bleeding"))
                {
                    statuscomp.RemoveEffect("Bleeding");   
                }
            }

            SendHealthUpdate();
        }

        /// <summary>
        /// Triggers bleeding if the damage is enough to warrant it.
        /// </summary>
        /// <param name="damageAmount"></param>
        /// <param name="damageType"></param>
        /// <param name="targetLocation"></param>
        protected void TriggerBleeding(int damageAmount, DamageType damageType, BodyPart targetLocation)
        {
            if (damageAmount < 1)
                return;
            double prob = (0.1f*damageAmount);
            switch (damageType)
            {
                case DamageType.Toxin:
                case DamageType.Burn:
                case DamageType.Untyped:
                case DamageType.Suffocation:
                case DamageType.Freeze:
                    prob = 0;
                    break;
                case DamageType.Piercing:
                    prob *= 1.1f;
                    break;
                case DamageType.Slashing:
                    prob *= 1.5f;
                    break;
                case DamageType.Bludgeoning:
                    prob *= 0.7f;
                    break;
            }

            switch (targetLocation)
            {
                case BodyPart.Groin:
                    prob *= 0.9f;
                    break;
                case BodyPart.Left_Arm:
                case BodyPart.Right_Arm:
                    prob *= 0.6f;
                    break;
                case BodyPart.Right_Leg:
                case BodyPart.Left_Leg:
                    prob *= 1f;
                    break;
                case BodyPart.Head:
                    prob *= 1.2f;
                    break;
                case BodyPart.Torso:
                    prob *= 1.1f;
                    break;
            }

            if (prob > 1)
            {
                var statuscomp = (StatusEffectComp) Owner.GetComponent(ComponentFamily.StatusEffects);
                statuscomp.AddEffect("Bleeding", Convert.ToUInt32(prob*10));
            }
        }

        protected override void ApplyDamage(IEntity damager, int damageamount, DamageType damType)
        {
            ApplyDamage(damager, damageamount, damType, BodyPart.Torso); //Apply randomly instead of chest only
        }

        protected override void ApplyDamage(int p)
        {
            ApplyDamage(Owner, p, DamageType.Untyped, BodyPart.Torso);
            ; //Apply randomly instead of chest only
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection client)
        {
            var type = (ComponentMessageType) message.MessageParameters[0];

            switch (type)
            {
                case (ComponentMessageType.HealthStatus):
                    SendHealthUpdate(client);
                    break;
            }
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = ComponentReplyMessage.Empty;
            if(type != ComponentMessageType.Damage)
                reply = base.RecieveMessage(sender, type, list);

            if (sender == this)
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.GetCurrentLocationHealth:
                    var location = (BodyPart) list[0];
                    if (damageZones.Exists(x => x.location == location))
                    {
                        DamageLocation dmgLoc = damageZones.First(x => x.location == location);
                        var reply1 = new ComponentReplyMessage(ComponentMessageType.CurrentLocationHealth, location,
                                                               dmgLoc.UpdateTotalHealth(), dmgLoc.maxHealth);
                        reply = reply1;
                    }
                    break;
                case ComponentMessageType.GetCurrentHealth:
                    var reply2 = new ComponentReplyMessage(ComponentMessageType.CurrentHealth, GetHealth(),
                                                           GetMaxHealth());
                    reply = reply2;
                    break;
                case ComponentMessageType.Damage:
                    if (list.Count() > 3) //We also have a target location
                        ApplyDamage((Entity) list[0], (int) list[1], (DamageType) list[2], (BodyPart) list[3]);
                    else //We dont have a target location
                        ApplyDamage((Entity) list[0], (int) list[1], (DamageType) list[2]);
                    break;
                case ComponentMessageType.Heal:
                    if (list.Count() > 3) // We also have a target location
                        ApplyHeal((Entity) list[0], (int) list[1], (DamageType) list[2], (BodyPart) list[3]);
                    break;
            }

            return reply;
        }

        public override float GetMaxHealth()
        {
            return damageZones.Sum(x => x.maxHealth);
        }

        public override float GetHealth()
        {
            return damageZones.Sum(x => x.UpdateTotalHealth());
        }

        protected override void SendHealthUpdate()
        {
            SendHealthUpdate(null);
        }

        protected override void SendHealthUpdate(NetConnection client)
        {
            foreach (DamageLocation loc in damageZones)
            {
                var newUp = new List<object>();
                newUp.Add(ComponentMessageType.HealthStatus);
                newUp.Add(loc.location);
                newUp.Add(loc.damageIndex.Count);
                newUp.Add(loc.maxHealth);
                foreach (var damagePair in loc.damageIndex)
                {
                    newUp.Add(damagePair.Key);
                    newUp.Add(damagePair.Value);
                }
                Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableOrdered,
                                                  client != null ? client : null, newUp.ToArray());
            }
        }
    }
}