using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.HelperClasses;
using System.Runtime.Serialization;

namespace SS3d_server.Atom.Item.Organs
{
    public class Organ
    {
        public string name;
        public Mob.Mob owner;
        public Organ masterConnection;
        public Type masterConnectionType;
        public List<Organ> externalChildren = new List<Organ>();
        public List<Organ> internalChildren = new List<Organ>();
        public int normalChildNumber;
        public Blood blood;
        public float max_blood = 0.0f; // The max amount of blood this organ can contain


        public Organ()
            : base()
        {
        }

        public virtual void SetUp(Mob.Mob _owner)
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

        public virtual void ConnectOrganTo(Mob.Mob mob)
        {
            if (!mob.organs.Contains(this))
                mob.organs.Add(this);
            owner = mob;
            ConnectOrgan();
        }

        public virtual void Process(float frametime)
        {
            if (owner == null)
                return;
            return;

        }

        public virtual void HeartBeat()
        {
            if (normalChildNumber > 0)
            {
                float shareBlood = ((blood.amount / max_blood) * (max_blood / 10)) / normalChildNumber;
                foreach (Organ organ in externalChildren)
                {
                    organ.HeartBeat();
                    if (organ.blood.amount < organ.max_blood)
                    {
                        float amount = Math.Min(organ.max_blood, organ.blood.amount - shareBlood) - organ.blood.amount;
                        if (organ.blood.CanRecieve(blood))
                            organ.blood.amount += amount;
                        else
                            organ.blood.amount -= amount;
                        blood.amount -= amount;
                    }
                }
                if (normalChildNumber < externalChildren.Count)
                {
                    blood.amount = Math.Max(0, blood.amount - (shareBlood * (2 * normalChildNumber - externalChildren.Count)));
                }
            }
        }

    }
}
