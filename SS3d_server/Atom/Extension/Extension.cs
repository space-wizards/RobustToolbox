using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS3d_server.Atom.Extension
{
    public abstract class Extension
    {
        protected Atom parentAtom;
        protected Extension()
        {
            Initialize();
        }

        protected abstract void Initialize();
        public abstract void Update(float framePeriod);
        public abstract void ApplyAction(Atom a, Mob.Mob m);
        public abstract void UsedOn(Atom target);
    }
}
