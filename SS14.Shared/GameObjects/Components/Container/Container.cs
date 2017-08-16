using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;

namespace SS14.Server.GameObjects.Components.Container
{
    public class Container : List<IEntity>, IContainer
    {
        public Container(IComponent holder)
        {
            IContainerManager containermanager = holder.Owner.GetComponent<IContainerManager>();
            if (containermanager == null)
            {
                var factory = IoCManager.Resolve<IComponentFactory>();
                holder.Owner.AddComponent(factory.GetComponent<ContainerManagerComponent>());
            }
            Owner = holder.Owner;
            if(containermanager.EntityContainers.ContainsKey(holder.GetType()))
            {
                throw new InvalidOperationException(); //This system is designed to have singular containers per component, make a new component dummy.
            }
            containermanager.EntityContainers[holder.GetType()] = this;
        }

        private IEntity Owner { get; set; }

        public virtual bool CanInsert(IEntity toinsert)
        {
            if (toinsert.GetComponent<ITransformComponent>().ContainsEntity(Owner.GetComponent<ITransformComponent>())) //Crucial, prevent circular insertion
            {
                return false;
            }
            return true;
        }

        public bool Insert(IEntity toinsert)
        {
            if (CanInsert(toinsert) && toinsert.GetComponent<ITransformComponent>().Parent.Owner.GetComponent<IContainerManager>().Remove(toinsert)) //Verify we can insert and that the object got properly removed from its current location
            {
                this.Add(toinsert);
                toinsert.GetComponent<ITransformComponent>().AttachParent(Owner.GetComponent<ITransformComponent>());
                //OnInsert(); If necessary a component may add eventhandlers for this and delegate some functions to it
                return true;
            }
            return false;
        }

        public virtual bool CanRemove(IEntity toremove)
        {
            if(!this.Contains(toremove))
            {
                return false;
            }
            return true;
        }

        public new bool Remove(IEntity toremove)
        {
            if (CanRemove(toremove))
            {
                base.Remove(toremove);
                toremove.GetComponent<ITransformComponent>().DetachParent();
                //OnRemoval(toremove); If necessary a component may add eventhandlers for this and delegate some functions to it
                return true;
            }
            return false;
        }

        public void Shutdown()
        {
            Owner = null;
            this.Clear();
        }
    }
}
