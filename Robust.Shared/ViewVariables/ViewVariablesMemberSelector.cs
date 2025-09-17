using System;
using System.Collections;
using System.Runtime.CompilerServices;
using Robust.Shared.Serialization;

namespace Robust.Shared.ViewVariables
{
    /// <summary>
    ///     When used as an index in <see cref="ViewVariablesSessionRelativeSelector.PropertyIndex"/>,
    ///     refers to a member (field or property) on the object of the session.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class ViewVariablesMemberSelector
    {
        public ViewVariablesMemberSelector(int index)
        {
            Index = index;
        }

        /// <summary>
        ///     The index of the member. These indices assigned by the server-side member trait.
        ///     It's an index instead of a dump string to solve the theoretical case of member hiding.
        /// </summary>
        public int Index { get; set; }
    }

    /// <summary>
    ///     When used as an index in <see cref="ViewVariablesSessionRelativeSelector.PropertyIndex"/>,
    ///     refers to an index in the results of a <see cref="IEnumerable"/>.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class ViewVariablesEnumerableIndexSelector
    {
        public ViewVariablesEnumerableIndexSelector(int index)
        {
            Index = index;
        }

        public int Index { get; set; }
    }

    /// <summary>
    /// Used within <see cref="ViewVariablesSessionRelativeSelector.PropertyIndex"/>
    /// to refer to an index of a tuple. This should match what would be used
    /// with <see cref="ITuple.get_Item"/>.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class ViewVariablesTupleIndexSelector(int index)
    {
        public int Index { get; set; } = index;
    }

    [Serializable, NetSerializable]
    public sealed class ViewVariablesSelectorKeyValuePair
    {
        // If false it's the value instead.
        public bool Key { get; set; }
    }

}
