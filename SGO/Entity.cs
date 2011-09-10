using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;

namespace SGO
{
    [SecuritySafeCritical]
    public class Entity
    {
        private Dictionary<ComponentFamily, IGameObjectComponent> components;
    }
}
