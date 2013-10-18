using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using GameObject;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;
using SS13_Shared.GO.Hands;

namespace SGO
{
    public class NewHandsComponent : Component
    {
        public Dictionary<InventoryLocation, Entity> handslots;
        public InventoryLocation currentHand = InventoryLocation.HandLeft;

        public NewHandsComponent()
        {
            Family = ComponentFamily.Hands;
            handslots = new Dictionary<InventoryLocation, Entity>();
        }

        public override void HandleExtendedParameters(XElement extendedParameters)
        {
            handslots.Clear();

            foreach (XElement param in extendedParameters.Descendants("handSlot"))
            {
                if (param.Attribute("slot") != null)
                    handslots.Add((InventoryLocation)Enum.Parse(typeof(InventoryLocation), param.Attribute("slot").Value), null);
            }
        }

        public bool RemoveEntity(Entity user, Entity toRemove)
        {
            if (handslots.Any(x => x.Value == toRemove))
            {
                handslots[handslots.First(x => x.Value == toRemove).Key] = null;
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool AddEntity(Entity user, Entity toAdd, InventoryLocation? whichHand = null)
        {
            if (handslots.Any(x => x.Value == toAdd))
            {
                return false;
            }
            else
            {
                if (whichHand.HasValue)
                {
                    if (handslots[whichHand.Value] != null)
                        return false;
                    else
                    {
                        handslots[whichHand.Value] = toAdd;
                    }
                }
                else
                {
                    if (handslots.Any(x => x.Value == null))
                    {
                        handslots[handslots.First(x => x.Value == null).Key] = toAdd;
                    }
                    else
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public override ComponentState GetComponentState()
        {
            //Oh man , what
            Dictionary<InventoryLocation, int> entities = handslots.Select(x => new KeyValuePair<InventoryLocation, int>(x.Key, x.Value.Uid)).ToDictionary(key => key.Key, va => va.Value);
            return new HandsState(currentHand, entities);
        }
    }
}