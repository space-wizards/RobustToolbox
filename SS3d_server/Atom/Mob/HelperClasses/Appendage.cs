using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3D_Server.Atom.Mob.HelperClasses
{
    public class Appendage
    {
        public string appendageName;
        public Item.Item heldItem = null;
        public Mob owner;
        public string attackAnimation;
        public int ID;

        public Appendage(string _appendageName, int _ID, Mob _owner)
        {
            appendageName = _appendageName;
            owner = _owner;
            attackAnimation = "tpose";
            ID = _ID;
        }

        public void AnimateAttack()
        {
            owner.AnimateOnce(attackAnimation);
        }
    }
}
