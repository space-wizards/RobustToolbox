using System;
using System.Collections;
using Robust.Shared.Serialization;

namespace Robust.Shared.ViewVariables
{
    /// <summary>
    ///     Pre-defined simple VV traits used by the engine.
    /// </summary>
    [Serializable, NetSerializable]
    public enum ViewVariablesTraits : byte
    {
        /// <summary>
        ///     This object has VV-accessible members (field or property with the <see cref="ViewVariablesAttribute"/>.
        /// </summary>
        Members,

        /// <summary>
        ///     This object implements <see cref="IEnumerable"/> and as such can be viewed as a sequence.
        /// </summary>
        Enumerable,

        /// <summary>
        ///     This object is an entity and has a list of components that can be viewed.
        /// </summary>
        Entity,
    }
}
