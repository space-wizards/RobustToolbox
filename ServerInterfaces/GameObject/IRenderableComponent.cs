using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using GameObject;
using SS13_Shared.GO;

namespace ServerInterfaces.GOC
{
    public interface IRenderableComponent : IComponent
    {
        bool IsSlaved();
        void SetMaster(Entity m);
        void UnsetMaster();
        void AddSlave(IRenderableComponent slavecompo);
        void RemoveSlave(IRenderableComponent slavecompo);
    }
}
