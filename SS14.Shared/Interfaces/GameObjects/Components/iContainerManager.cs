using SS14.Shared.Interfaces.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Server.Interfaces.GameObjects
{
    public interface IContainerManager : IComponent
    {
        bool Remove(IEntity entity);
        Dictionary<Type, IContainer> EntityContainers { get; }
    }
}
