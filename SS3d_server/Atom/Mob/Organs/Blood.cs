using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3d_server.Atom.Item.Organs
{
    /*
    Donor : Compatible recipitents
    A: A AB
    B B AB
    AB: AB
    O: A B AB O
    */
    public enum BLOOD_TYPE
    {
        A = 0,
        B,
        AB,
        O
    }

    public class Blood
    {
        public BLOOD_TYPE blood_type;
        public float amount = 0;

        public Blood(BLOOD_TYPE _blood_type, float _amount)
        {
            blood_type = _blood_type;
            amount = _amount;
        }

        public bool CanDonateTo(Blood blood)
        {
            switch (blood_type)
            {
                case BLOOD_TYPE.A:
                    if (blood.blood_type == BLOOD_TYPE.A || blood.blood_type == BLOOD_TYPE.AB)
                        return true;
                    return false;
                case BLOOD_TYPE.B:
                    if (blood.blood_type == BLOOD_TYPE.B || blood.blood_type == BLOOD_TYPE.AB)
                        return true;
                    return false;
                case BLOOD_TYPE.AB:
                    if (blood.blood_type == BLOOD_TYPE.AB)
                        return true;
                    return false;
                case BLOOD_TYPE.O:
                    return true;
                default:
                    return false;
            }
        }

        public bool CanRecieve(Blood blood)
        {
            return blood.CanDonateTo(this);
        }
    }
}
