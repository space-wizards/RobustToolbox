using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using GameObject;
using Lidgren.Network;
using SGO.Item.ItemCapability;
using SS13_Shared.GO;

namespace SGO
{
    public class BasicItemComponent : Component
    {
        private readonly Dictionary<string, ItemCapability> capabilities;
        private Hand holdingHand;

        public BasicItemComponent()
        {
            Family = ComponentFamily.Item;
            capabilities = new Dictionary<string, ItemCapability>();
        }

        public Entity currentHolder { get; private set; }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

            if (sender == this)
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.ReceiveEmptyHandToItemInteraction:
                    HandleEmptyHandToItemInteraction((Entity) list[0]);
                    // param 0 is the actor entity, param 1 is the source actor entity
                    break;
                case ComponentMessageType.ReceiveItemToItemInteraction:
                    //This message means we were clicked on by an actor with an item in hand
                    HandleItemToItemInteraction((Entity) list[0]);
                    // param 0 is the actor entity, param 1 is the source actor entity
                    break;
                case ComponentMessageType.EnactItemToActorInteraction:
                    ApplyTo((Entity) list[0], InteractsWith.Actor, (Entity) list[1]);
                    break;
                case ComponentMessageType.EnactItemToItemInteraction:
                    ApplyTo((Entity) list[0], InteractsWith.Item, (Entity) list[1]);
                    break;
                case ComponentMessageType.EnactItemToLargeObjectInteraction:
                    ApplyTo((Entity) list[0], InteractsWith.LargeObject, (Entity) list[1]);
                    break;
                case ComponentMessageType.PickedUp:
                    HandlePickedUp((Entity) list[0], (Hand) list[1]);
                    break;
                case ComponentMessageType.Dropped:
                    HandleDropped();
                    break;
                case ComponentMessageType.ItemGetCapability:
                    ItemCapability[] itemcaps = GetCapability((ItemCapabilityType) list[0]);
                    if (itemcaps != null)
                        reply = new ComponentReplyMessage(ComponentMessageType.ItemReturnCapability, itemcaps);
                    break;
                case ComponentMessageType.ItemGetCapabilityVerbPairs:
                    var verbpairs = new List<KeyValuePair<ItemCapabilityType, ItemCapabilityVerb>>();
                    foreach (ItemCapability capability in capabilities.Values)
                    {
                        foreach (
                            ItemCapabilityVerb verb in
                                (from v in capability.verbs orderby v.Key descending select v.Value))
                        {
                            verbpairs.Add(
                                new KeyValuePair<ItemCapabilityType, ItemCapabilityVerb>(capability.CapabilityType, verb));
                        }
                    }
                    //if(verbpairs.Count > 0)
                    reply = new ComponentReplyMessage(ComponentMessageType.ItemReturnCapabilityVerbPairs,
                                                      verbpairs.ToLookup(v => v.Key, v => v.Value));
                    break;
                case ComponentMessageType.CheckItemHasCapability:
                    reply = new ComponentReplyMessage(ComponentMessageType.ItemHasCapability,
                                                      HasCapability((ItemCapabilityType) list[0]));
                    break;
                case ComponentMessageType.ItemGetAllCapabilities:
                    reply = new ComponentReplyMessage(ComponentMessageType.ItemReturnCapability, (object)GetAllCapabilities());
                    break;
                case ComponentMessageType.Activate:
                    Activate();
                    break;
                case ComponentMessageType.ClickedInHand:
                    Activate();
                    break;
            }

            return reply;
        }
        
        /// <summary>
        /// Applies this item to the target entity. 
        /// </summary>
        /// <param name="targetEntity">Target entity</param>
        /// <param name="targetType">Type of entity, Item, LargeObject, or Actor</param>
        protected virtual void ApplyTo(Entity targetEntity, InteractsWith targetType, Entity sourceActor)
        {
            //can be overridden in children to sort of bypass the capability system if needed.
            ApplyCapabilities(targetEntity, targetType, sourceActor);
        }

        private void HandleDropped()
        {
            Owner.RemoveComponent(ComponentFamily.Mover);
            Owner.AddComponent(ComponentFamily.Mover, Owner.EntityManager.ComponentFactory.GetComponent("BasicMoverComponent"));
            Owner.SendDirectedComponentNetworkMessage(this, NetDeliveryMethod.ReliableOrdered, null,
                                              ItemComponentNetMessage.Dropped);
            currentHolder = null;
        }

        private void HandlePickedUp(Entity entity, Hand _holdingHand)
        {
            currentHolder = entity;
            holdingHand = _holdingHand;
            Owner.AddComponent(ComponentFamily.Mover, Owner.EntityManager.ComponentFactory.GetComponent("SlaveMoverComponent"));
            Owner.SendMessage(this, ComponentMessageType.SlaveAttach, entity.Uid);
            Owner.SendDirectedComponentNetworkMessage(this, NetDeliveryMethod.ReliableOrdered, null,
                                              ItemComponentNetMessage.PickedUp, entity.Uid, holdingHand);
        }

        public override void HandleInstantiationMessage(NetConnection netConnection)
        {
            if (currentHolder != null)
                Owner.SendDirectedComponentNetworkMessage(this, NetDeliveryMethod.ReliableOrdered, netConnection,
                                                  ItemComponentNetMessage.PickedUp, currentHolder.Uid, holdingHand);
        }

        /// <summary>
        /// Entry point for interactions between an item and this item
        /// Basically, the actor uses an item on this item
        /// </summary>
        /// <param name="entity">The actor entity</param>
        protected virtual void HandleItemToItemInteraction(Entity actor)
        {
            //Get the item

            //Apply actions based on the item's types
            //Message the item to tell it to apply whatever it needs to do as well
        }

        /// <summary>
        /// Entry point for interactions between an empty hand and this item
        /// Basically, the actor touches this item with an empty hand
        /// </summary>
        /// <param name="actor"></param>
        protected virtual void HandleEmptyHandToItemInteraction(Entity actor)
        {
            //Pick up the item
            actor.SendMessage(this, ComponentMessageType.PickUpItem, Owner);
        }

        private void Activate()
        {
            Owner.SendMessage(this, ComponentMessageType.Activate);
            ActivateCapabilities();
        }

        /// <summary>
        /// Apply this item's capabilities to a target entity
        /// This finds all onboard capability modules that can interact with a given object type, 
        /// sorted by priority. Only one thing will actually execute, depending on priority. 
        /// ApplyTo returns true if it successfully interacted with the target, false if not.
        /// </summary>
        /// <param name="target">Target entity for interaction</param>
        protected virtual void ApplyCapabilities(Entity target, InteractsWith targetType, Entity sourceActor)
        {
            IOrderedEnumerable<ItemCapability> capstoapply = from c in capabilities.Values
                                                             where (c.interactsWith & targetType) == targetType
                                                             orderby c.priority descending
                                                             select c;

            foreach (ItemCapability capability in capstoapply)
            {
                if (capability.ApplyTo(target, sourceActor))
                    break;
            }
        }

        /// <summary>
        /// Executes a query to get the capabilities of this item.
        /// This enables us to ask things like, "is the item a tool?"
        /// You can have items that have more than one tool capability module, and return them both via a query.
        /// This is mostly useful for the object that is receiving an action from the item.
        /// </summary>
        /// <param name="query">ItemCapabilityQuery object</param>
        /// <returns></returns>
        protected ItemCapabilityQueryResult ExecuteCapabilityQuery(ItemCapabilityQuery query)
        {
            var result = new ItemCapabilityQueryResult();
            result.ResultStatus = ItemCapabilityQueryResult.ItemCapabilityQueryResultType.Error;
            switch (query.queryType)
            {
                case ItemCapabilityQuery.ItemCapabilityQueryType.GetAllCapabilities:
                    //Get all the capabilities of the object. Not particularly useful.
                    foreach (ItemCapability c in capabilities.Values)
                        result.AddCapability(c);
                    if (capabilities.Count() > 0)
                        result.ResultStatus = ItemCapabilityQueryResult.ItemCapabilityQueryResultType.Success;
                    else
                        result.ResultStatus = ItemCapabilityQueryResult.ItemCapabilityQueryResultType.Empty;
                    break;
                case ItemCapabilityQuery.ItemCapabilityQueryType.GetCapability:
                    //Get the capabilities that match a particular capability type
                    IEnumerable<ItemCapability> caps = from c in capabilities.Values
                                                       where (c.CapabilityType == query.capabilityType)
                                                       select c;
                    foreach (ItemCapability c in caps)
                        result.AddCapability(c);
                    if (caps.Count() > 0)
                        result.ResultStatus = ItemCapabilityQueryResult.ItemCapabilityQueryResultType.Success;
                    else
                        result.ResultStatus = ItemCapabilityQueryResult.ItemCapabilityQueryResultType.Empty;
                    break;
                case ItemCapabilityQuery.ItemCapabilityQueryType.HasCapability:
                    //Check if the item has a capability of a certain type.
                    IEnumerable<ItemCapability> hascap = from c in capabilities.Values
                                                         where (c.CapabilityType == query.capabilityType)
                                                         select c;
                    if (hascap.Count() > 0)
                        result.ResultStatus = ItemCapabilityQueryResult.ItemCapabilityQueryResultType.True;
                    else
                        result.ResultStatus = ItemCapabilityQueryResult.ItemCapabilityQueryResultType.False;
                    break;
            }
            if (result.ResultStatus == ItemCapabilityQueryResult.ItemCapabilityQueryResultType.Error)
                result.ErrorMessage = "No Result";
            return result;
        }

        private ItemCapability[] GetCapability(ItemCapabilityType type)
        {
            ItemCapabilityQueryResult result =
                ExecuteCapabilityQuery(new ItemCapabilityQuery(
                                           ItemCapabilityQuery.ItemCapabilityQueryType.GetCapability, type));
            if (result.ResultStatus == ItemCapabilityQueryResult.ItemCapabilityQueryResultType.Success)
                return result.Capabilities;
            else
                return null;
        }

        private ItemCapability[] GetAllCapabilities()
        {
            ItemCapabilityQueryResult result =
                ExecuteCapabilityQuery(
                    new ItemCapabilityQuery(ItemCapabilityQuery.ItemCapabilityQueryType.GetAllCapabilities,
                                            ItemCapabilityType.None));
            if (result.ResultStatus == ItemCapabilityQueryResult.ItemCapabilityQueryResultType.Empty)
                return new ItemCapability[0];
            else
                return result.Capabilities;
        }

        private void ActivateCapabilities()
        {
            foreach(var c in GetAllCapabilities())
            {
                c.Activate();
            }
        }

        private bool HasCapability(ItemCapabilityType type)
        {
            ItemCapabilityQueryResult result =
                ExecuteCapabilityQuery(new ItemCapabilityQuery(
                                           ItemCapabilityQuery.ItemCapabilityQueryType.HasCapability, type));
            if (result.ResultStatus == ItemCapabilityQueryResult.ItemCapabilityQueryResultType.True)
                return true;
            else
                return false;
        }

        public void AddCapability(ItemCapability cap)
        {
            capabilities.Add(cap.capabilityName, cap);
            cap.owner = this;
        }

        public override void HandleExtendedParameters(XElement extendedParameters)
        {
            foreach (XElement itemcapability in extendedParameters.Descendants("ItemCapability"))
            {
                IEnumerable<XElement> Verbs = itemcapability.Descendants("ItemCapabilityVerb");
                IEnumerable<XElement> Parameters = itemcapability.Descendants("ItemCapabilityParameter");
                ItemCapability cap = null;
                switch (itemcapability.Attribute("name").Value)
                {
                    case "MeleeWeaponCapability":
                        cap = new MeleeWeaponCapability();
                        break;
                    case "ToolCapability":
                        cap = new ToolCapability();
                        break;
                    case "GunCapability":
                        cap = new GunCapability();
                        break;
                    case "MedicalCapability":
                        cap = new MedicalCapability();
                        break;
                    case "HealthScanCapability":
                        cap = new HealthScanCapability();
                        break;
                    case "BreatherCapability":
                        cap = new BreatherCapability();
                        break;
                }

                if (cap == null)
                    return;
                foreach (XElement verb in Verbs)
                {
                    cap.AddVerb(int.Parse(verb.Attribute("priority").Value),
                                (ItemCapabilityVerb)
                                Enum.Parse(typeof (ItemCapabilityVerb), verb.Attribute("name").Value));
                }
                foreach (XElement parameter in Parameters)
                {
                    string name = parameter.Attribute("name").Value;
                    Type type = EntityTemplate.TranslateType(parameter.Attribute("type").Value);
                    var value = Convert.ChangeType(parameter.Attribute("value").Value, type);

                    var cparam = new ComponentParameter(name, value);
                    cap.SetParameter(cparam);
                }
                AddCapability(cap);
            }
        }
    }
}