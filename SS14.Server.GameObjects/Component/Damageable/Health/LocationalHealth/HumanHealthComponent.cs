using Lidgren.Network;
using SS14.Server.Interfaces.Map;
using SS14.Server.Interfaces.Player;
using SS14.Server.Interfaces.Tiles;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.GO.Component.Damageable.Health.LocationalHealth;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SS14.Server.GameObjects
{
    /// <summary>
    /// Handles mob health, with locational damage
    /// </summary>
    public class HumanHealthComponent : HealthComponent
    {
        private DateTime _lastUpdate;
        protected List<DamageLocation> damageZones = new List<DamageLocation>();

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

            var statuscomp = Owner.GetComponent<StatusEffectComp>(ComponentFamily.StatusEffects);
            if (statuscomp == null)
                return;

            ITile t = map.GetFloorAt(Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position);

            if (t == null)
            {
                statuscomp.AddEffect("Hypoxia", 5); //Out of map bounds, you is asphyxiatin to death bitch
            }
            else
            {
                bool hasInternals = HasInternals();

                if (t.GasCell.GasAmount(GasType.Toxin) > 0.01 && !hasInternals)
                    //too much toxin in the air, bro
                {
                    statuscomp.AddEffect("ToxinInhalation", 20);
                }
                if (!hasInternals && t.GasCell.Pressure < 10 //Less than 10kPa
                    ||
                    (t.GasCell.GasAmount(GasType.Oxygen)/
                     t.GasCell.TotalGas) < 0.10f) //less than 10% oxygen
                    //Not enough oxygen in the mixture, or pressure is too low.
                    statuscomp.AddEffect("Hypoxia", 5);
            }
        }

        // TODO use state system
        public override void HandleInstantiationMessage(NetConnection netConnection)
        {
            SendHealthUpdate(netConnection);
        }

        protected void ApplyDamage(Entity damager, int damageamount, DamageType damType, BodyPart targetLocation)
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

            if (targetLocation == BodyPart.Head && actualDamage > 5)
            {
                ComponentReplyMessage r = Owner.SendMessage(this, ComponentFamily.Actor,
                                                            ComponentMessageType.GetActorSession);
                if (r.MessageType == ComponentMessageType.ReturnActorSession)
                {
                    var s = (IPlayerSession) r.ParamsList[0];
                    s.AddPostProcessingEffect(PostProcessingEffectType.Blur, 5);
                }
            }

            TriggerBleeding(damageamount, damType, targetLocation);

            currentHealth = GetHealth();
            maxHealth = GetMaxHealth();

            SendHealthUpdate();

            if (GetHealth() <= 0)
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
                dmgLoc.HealDamage(damType, (int) realHealAmount);
            }

            currentHealth = GetHealth();
            maxHealth = GetMaxHealth();

            if (damType == DamageType.Slashing)
            {
                var statuscomp = (StatusEffectComp) Owner.GetComponent(ComponentFamily.StatusEffects);
                if (statuscomp.HasEffect("Bleeding"))
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

        protected override void ApplyDamage(Entity damager, int damageamount, DamageType damType)
        {
            ApplyDamage(damager, damageamount, damType, BodyPart.Torso); //Apply randomly instead of chest only
        }

        protected override void ApplyDamage(int p)
        {
            ApplyDamage(Owner, p, DamageType.Untyped, BodyPart.Torso);
            ; //Apply randomly instead of chest only
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = ComponentReplyMessage.Empty;
            if (type != ComponentMessageType.Damage)
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

        protected bool HasInternals()
        {
            if (Owner.HasComponent(ComponentFamily.Equipment))
            {
                return Owner.GetComponent<EquipmentComponent>(ComponentFamily.Equipment).HasInternals();
            }
            // Maybe this should return false? If there's no equipment component?
            return true;
        }

        public override ComponentState GetComponentState()
        {
            return new HumanHealthComponentState(isDead, GetHealth(), GetMaxHealth(), GetLocationHealthStates());
        }

        private List<LocationHealthState> GetLocationHealthStates()
        {
            var list = new List<LocationHealthState>();
            foreach (DamageLocation loc in damageZones)
            {
                var state = new LocationHealthState();
                state.Location = loc.location;
                state.MaxHealth = loc.maxHealth;
                state.CurrentHealth = loc.currentHealth;
                foreach (var damagePair in loc.damageIndex)
                {
                    state.DamageIndex.Add(damagePair.Key, damagePair.Value);
                }
                list.Add(state);
            }
            return list;
        }
    }
}