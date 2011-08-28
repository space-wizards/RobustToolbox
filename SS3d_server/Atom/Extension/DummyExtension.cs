using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3D_Server.Atom.Extension
{
    public class DummyExtension : Extension
    {
        public DummyExtension(Atom _parentAtom)
        {
            parentAtom = _parentAtom;
            Initialize();
        }

        protected override void Initialize()
        {
            
        }

        public override void Update(float framePeriod)
        {

        }
        public override void ApplyAction(Atom a, Mob.Mob m)
        {
            //Console.Write("DummyExtension: " + a.name + " " + a.uid.ToString() + " applied to " + parentAtom.name + " " + parentAtom.uid.ToString() + "\n");
        }
        public override void UsedOn(Atom target)
        {
            //Console.Write("DummyExtension: " + parentAtom.name + " " + parentAtom.uid.ToString() + " used on " + target.name + " " + target.uid.ToString() + "\n");
        }
    }
}
