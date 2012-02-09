using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared;
using SS13_Shared.GO;

namespace SGO.Component.Item.ItemCapability
{
    public class ItemCapability
    {
        public InteractsWith interactsWith; //What types of shit this interacts with
        public int priority; //Where in the stack this puppy is
        protected ItemCapabilityType capabilityType;
        public GameObjectComponent owner;
        public ItemCapabilityType CapabilityType
        {
            get { return capabilityType; }
            protected set { capabilityType = value; }
        }

        public string capabilityName;

        /// <summary>
        /// dictionary of priority -> verb -- lower priority verbs execute first
        /// </summary>
        public Dictionary<int, ItemCapabilityVerb> verbs;

        public ItemCapability()
        {
            verbs = new Dictionary<int, ItemCapabilityVerb>();
        }

        public virtual bool ApplyTo(Entity target, Entity sourceActor)
        {
            return false;
        }

        public void AddVerb(int priority, ItemCapabilityVerb verb)
        {
            if (verbs.ContainsKey(priority)) //Shuffle the list to insert the specified verb and move the one in that spot down.
            {
                var tverb = verbs[priority];
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
    }

    /// <summary>
    /// Query datatype
    /// </summary>
    public struct ItemCapabilityQuery
    {
        public ItemCapabilityQueryType queryType;
        public ItemCapabilityType capabilityType;
        public ItemCapabilityQuery(ItemCapabilityQueryType _queryType, ItemCapabilityType _capabilityType)
        {
            queryType = _queryType;
            capabilityType = _capabilityType;
        }

        public enum ItemCapabilityQueryType
        {
            HasCapability,
            GetCapability,
            GetAllCapabilities,
        }
    }

    /// <summary>
    /// Query result datatype
    /// </summary>
    public class ItemCapabilityQueryResult
    {
        private ItemCapabilityQueryResultType resultStatus;
        public ItemCapabilityQueryResultType ResultStatus
        {
            get 
            { 
                if(resultStatus == null)
                    return ItemCapabilityQueryResultType.Null;
                else 
                    return resultStatus;
            }
            set { resultStatus = value; }
        }

        private string errorMessage;
        public string ErrorMessage
        {
            get { return errorMessage; }
            set { errorMessage = value; }
        }

        private ItemCapability[] returnedCapabilities;

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
            var retcap = returnedCapabilities.ToList();
            retcap.Add(cap);
            returnedCapabilities = retcap.ToArray();
        }
        
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
    }
}
