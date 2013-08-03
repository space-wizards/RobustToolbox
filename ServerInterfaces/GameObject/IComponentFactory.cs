using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameObject;

namespace ServerInterfaces.GOC
{
    public interface IComponentFactory
    {
        Component GetComponent(string componentType);
    }
}
