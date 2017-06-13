using Lidgren.Network;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.Inventory;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SS14.Client.GameObjects
{
    [IoCTarget]
    public class InventoryComponent : ClientComponent
    {
        public override string Name => "Inventory";
        #region Delegates

        public delegate void InventoryComponentUpdateHandler(
            InventoryComponent sender, int maxSlots, List<IEntity> entities);

        #endregion Delegates

        public InventoryComponent()
        {
            Family = ComponentFamily.Inventory;
            ContainedEntities = new List<IEntity>();
        }

        public List<IEntity> ContainedEntities { get; private set; }

        public int MaxSlots { get; private set; }

        public override Type StateType
        {
            get { return typeof(InventoryComponentState); }
        }

        public event InventoryComponentUpdateHandler Changed;

        public bool ContainsEntity(IEntity entity)
        {
            return ContainedEntities.Contains(entity);
        }

        public bool ContainsEntity(string templatename)
        {
            return ContainedEntities.Exists(x => x.Prototype.ID == templatename);
        }

        public IEntity GetEntity(string templatename)
        {
            return ContainedEntities.FirstOrDefault(x => x.Prototype.ID == templatename);
        }

        // TODO raise an event to be handled by a clientside InventorySystem, which will send the event through to the server?
        public void SendInventoryAdd(IEntity ent)
        {
            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered,
                                              ComponentMessageType.InventoryAdd, ent.Uid);
        }

        public override void HandleComponentState(dynamic state)
        {
            var theState = state as InventoryComponentState;
            var stateChanged = false;
            if (MaxSlots != theState.MaxSlots)
            {
                MaxSlots = theState.MaxSlots;
                stateChanged = true;
            }

            var newEntities = new List<int>(theState.ContainedEntities);
            var toRemove = new List<IEntity>();
            foreach (var e in ContainedEntities)
            {
                if (newEntities.Contains(e.Uid))
                {
                    newEntities.Remove(e.Uid);
                }
                else
                {
                    toRemove.Add(e);
                }
            }
            stateChanged = stateChanged || toRemove.Any() || newEntities.Any();
            foreach (var e in toRemove)
            {
                ContainedEntities.Remove(e);
            }

            foreach (var uid in newEntities)
            {
                ContainedEntities.Add(Owner.EntityManager.GetEntity(uid));
            }

            if (stateChanged && Changed != null) Changed(this, MaxSlots, ContainedEntities);
        }
    }
}
