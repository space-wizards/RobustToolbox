using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Shared.GameObjects
{
    /// <summary>
    ///     A version of component with built-in network syncing.
    /// </summary>
    public abstract class NetComponent : Component
    {
        //C# does not have friend classes :(
        /// <summary>
        ///     Contains a primitive type that is synced over networking to connected clients.
        /// </summary>
        protected class NetVar<T>
        {
            private NetComponent _owner;
            private int _index;

            /// <summary>
            ///     Only use this for serialization. Setting this does not keep
            ///     the value synced over the network. DO NOT USE THIS FOR NORMAL
            ///     CODE. 
            /// </summary>
            internal T Field;

            /// <summary>
            ///     The value that the NetVar holds. For normal operations use this property.
            /// </summary>
            public T Value
            {
                get => Field;
                set
                {
                    // string has to be such a special little snowflake...
                    if(value == null && Field == null)
                        return;

                    if(value != null && value.Equals(Field))
                        return;

                    Field = value;
                    _owner.NetVarChanged(_index, Field);
                }
            }
        }

        private void NetVarChanged(int index, object newValue)
        {
            //TODO: NetSync
        }
    }
}
