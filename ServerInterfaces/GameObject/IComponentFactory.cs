using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerInterfaces.GameObject
{
    public interface IComponentFactory
    {
        IGameObjectComponent GetComponent(string componentType);
    }
}
