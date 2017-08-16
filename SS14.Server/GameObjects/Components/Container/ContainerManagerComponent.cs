using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Server.GameObjects.Components.Container
{
    public class ContainerManagerComponent : Component, IContainerManager
    {
        public override string Name => "ContainerContainer";

        public Dictionary<Type, IContainer> EntityContainers = new Dictionary<Type, IContainer>();
        Dictionary<Type, IContainer> IContainerManager.EntityContainers => EntityContainers;

        public bool Remove(IEntity entity)
        {
            foreach (var containers in EntityContainers.Values)
            {
                if (containers.Contains(entity))
                {
                    if (containers.Remove(entity))
                    {
                        return true;
                    }
                    return false;
                }
            }
            return false;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            foreach(var container in EntityContainers.Values)
            {
                container.Shutdown();
            }
            EntityContainers.Clear();
        }
    }
}
