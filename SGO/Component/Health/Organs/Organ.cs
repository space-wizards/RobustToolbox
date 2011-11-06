using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.HelperClasses;
using SS3D_shared.GO;
using System.Runtime.Serialization;

namespace SGO.Component.Health.Organs
{
    public class Organ
    {
        public string name;
        public HealthComponent owner;
        public Organ masterConnection;
        public Type masterConnectionType;
        public List<Organ> externalChildren = new List<Organ>();
        public List<Organ> internalChildren = new List<Organ>();
        public int normalChildNumber;
        public Blood blood;
        public float max_blood = 0.0f; // The max amount of blood this organ can contain
        private bool heartBeat = false;


        public Organ()
            : base()
        {
        }

        public virtual void SetUp(HealthComponent _owner)
        {
            owner = _owner;
            blood = new Blood(owner.blood_type, max_blood);
            ConnectOrgan();
        }

        public virtual void ConnectOrgan()
        {
            if (owner == null)
                return;
            foreach (Organs.External.External O in owner.organs)
            {
                if (O.GetType() == masterConnectionType)
                {
                    masterConnection = O;
                    O.externalChildren.Add(this);
                    break;
                }
            }
        }

        public virtual void ConnectOrganTo(HealthComponent hc)
        {
            if (!hc.organs.Contains(this))
                hc.organs.Add(this);
            owner = hc;
            ConnectOrgan();
        }

        public virtual void Process(float frametime)
        {
            heartBeat = false;
            if (owner == null)
                return;
            return;
        }

        public virtual void HeartBeat()
        {
            if (heartBeat)
                return;
            heartBeat = true;

            if (normalChildNumber > 0)
            {

                if (normalChildNumber < externalChildren.Count)
                {
                    float lostBlood = ((blood.amount / max_blood) * (max_blood / 10)) / normalChildNumber;
                    blood.amount = Math.Max(0, blood.amount - (lostBlood * (2 * normalChildNumber - externalChildren.Count)));
                }
                foreach (Organ organ in externalChildren)
                {
                    if (!organ.heartBeat)
                    {
                        organ.HeartBeat();
                        ShareBloodWith(organ);
                    }
                }
            }
        }

        public virtual void Damage(Entity damager, int damAmount, DamageType damType)
        {
            RemoveBlood(damAmount);
        }

        public void RemoveBlood(float amount)
        {
            blood.amount = Math.Max(0, blood.amount - amount);
        }

        public void AddBlood(float amount)
        {
            blood.amount = Math.Min(max_blood, blood.amount + amount);
        }

        private void ShareBloodWith(Organ organ)
        {
            float myFactor = blood.amount / max_blood;
            float organFactor = organ.blood.amount / organ.max_blood;
            float missing = max_blood - blood.amount;
            if (myFactor < organFactor) // We need to take blood
            {
                float amount = Math.Min(missing, organ.blood.amount * ((organFactor - myFactor) / normalChildNumber));
                AddBlood(amount);
                organ.RemoveBlood(amount);
            }
            else // We need to give blood
            {
                float amount = Math.Min(missing, blood.amount * (( myFactor - organFactor) / normalChildNumber));
                RemoveBlood(amount);
                organ.AddBlood(amount);
            }
        }
    }
}
