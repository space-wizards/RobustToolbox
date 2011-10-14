using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.GO;

namespace SGO.Component.Item.ItemCapability
{
    public class ItemCapability
    {
        public InteractsWith interactsWith; //What types of shit this interacts with
        public int priority; //Where in the stack this puppy is
        private ItemCapabilityType capabilityType;
        public ItemCapabilityType CapabilityType
        {
            get { return capabilityType; }
            protected set { capabilityType = value; }
        }


        public bool ApplyTo(Entity target)
        {
            throw new NotImplementedException();
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
