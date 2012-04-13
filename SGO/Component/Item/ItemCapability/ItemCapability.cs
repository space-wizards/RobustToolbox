using System.Collections.Generic;
using System.Linq;
using SS13_Shared.GO;

namespace SGO.Component.Item.ItemCapability
{
    public class ItemCapability
    {
        public string capabilityName;
        protected ItemCapabilityType capabilityType;
        public InteractsWith interactsWith; //What types of shit this interacts with
        public GameObjectComponent owner;
        public int priority; //Where in the stack this puppy is

        /// <summary>
        /// dictionary of priority -> verb -- lower priority verbs execute first
        /// </summary>
        public Dictionary<int, ItemCapabilityVerb> verbs;

        public ItemCapability()
        {
            verbs = new Dictionary<int, ItemCapabilityVerb>();
        }

        public ItemCapabilityType CapabilityType
        {
            get { return capabilityType; }
            protected set { capabilityType = value; }
        }

        public virtual bool ApplyTo(Entity target, Entity sourceActor)
        {
            return false;
        }

        public void AddVerb(int priority, ItemCapabilityVerb verb)
        {
            if (verbs.ContainsKey(priority))
                //Shuffle the list to insert the specified verb and move the one in that spot down.
            {
                ItemCapabilityVerb tverb = verbs[priority];
                RemoveVerb(priority);
                AddVerb(priority, verb);
                AddVerb(priority + 1, tverb);
            }
            else
                verbs.Add(priority, verb);
        }

        public void RemoveVerb(int priority)
        {
            verbs.Remove(priority);
        }

        /// <summary>
        /// This allows setting of the component's parameters once it is instantiated.
        /// This should basically be overridden by every inheriting component, as parameters will be different
        /// across the board.
        /// </summary>
        /// <param name="parameter">ComponentParameter object describing the parameter and the value</param>
        public virtual void SetParameter(ComponentParameter parameter)
        {
        }

        public virtual void Activate()
        {
            
        }
    }

    /// <summary>
    /// Query datatype
    /// </summary>
    public struct ItemCapabilityQuery
    {
        #region ItemCapabilityQueryType enum

        public enum ItemCapabilityQueryType
        {
            HasCapability,
            GetCapability,
            GetAllCapabilities,
        }

        #endregion

        public ItemCapabilityType capabilityType;
        public ItemCapabilityQueryType queryType;

        public ItemCapabilityQuery(ItemCapabilityQueryType _queryType, ItemCapabilityType _capabilityType)
        {
            queryType = _queryType;
            capabilityType = _capabilityType;
        }
    }

    /// <summary>
    /// Query result datatype
    /// </summary>
    public class ItemCapabilityQueryResult
    {
        #region ItemCapabilityQueryResultType enum

        /// <summary>
        /// Types of results
        /// </summary>
        public enum ItemCapabilityQueryResultType
        {
            True,
            False,
            Success,
            Empty,
            Error,
            Null
        }

        #endregion

        private ItemCapabilityQueryResultType resultStatus;
        private ItemCapability[] returnedCapabilities;

        public ItemCapabilityQueryResultType ResultStatus
        {
            get
            {
                if (resultStatus == null)
                    return ItemCapabilityQueryResultType.Null;
                else
                    return resultStatus;
            }
            set { resultStatus = value; }
        }

        public string ErrorMessage { get; set; }

        public ItemCapability[] Capabilities
        {
            get { return returnedCapabilities; }
        }

        /// <summary>
        /// Adds a capability to the query result to be returned.
        /// </summary>
        /// <param name="cap"></param>
        public void AddCapability(ItemCapability cap)
        {
            List<ItemCapability> retcap = returnedCapabilities.ToList();
            retcap.Add(cap);
            returnedCapabilities = retcap.ToArray();
        }
    }
}