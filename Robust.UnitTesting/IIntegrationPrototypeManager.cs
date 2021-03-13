using System;
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Result;

namespace Robust.UnitTesting
{
    public interface IIntegrationPrototypeManager : IPrototypeManager
    {
        /// <summary>
        ///     Used in integration tests to ensure that extra prototypes
        ///     are loaded.
        ///     Mounting a string onto the resource manager does not work
        ///     as that appends it in the content root, adding it to
        ///     the virtual Prototypes folder.
        ///     That is skipped as it is cached in an integration prototype
        ///     manager to reduce run time.
        /// </summary>
        /// <param name="str">The string to load, containing the extra prototypes.</param>
        void QueueLoadString(string str);

        void Resync(
            Dictionary<Type, PrototypeInheritanceTree> trees,
            Dictionary<Type, int> priorities,
            Dictionary<Type, Dictionary<string, DeserializationResult>> results,
            Dictionary<Type, Dictionary<string, IPrototype>> prototypes);
    }
}
