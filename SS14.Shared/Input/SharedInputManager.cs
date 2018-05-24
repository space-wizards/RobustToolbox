using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SS14.Shared.Input
{
    /// <summary>
    ///     Assigns IDs to key functions for referencing over the network efficiently.
    /// </summary>
    /// <remarks>
    ///     Key functions are pulled from static readonly <see cref="BoundKeyFunction" /> fields on all classes marked with <see cref="KeyFunctionsAttribute" />
    /// </remarks>
    public class SharedInputManager
    {
        [Dependency]
        private readonly IReflectionManager reflectionManager;

        private readonly Dictionary<BoundKeyFunction, int> KeyFunctionsMap = new Dictionary<BoundKeyFunction, int>();

        protected void PopulateKeyFunctionsMap()
        {
            if (KeyFunctionsMap.Count != 0)
            {
                throw new InvalidOperationException("Cannot run this method twice.");
            }

            var tempList = new List<BoundKeyFunction>();
            foreach (var type in reflectionManager.FindTypesWithAttribute<KeyFunctionsAttribute>())
            {
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
                {
                    // This check makes sure we ONLY get readonly fields with the type we want.
                    // https://stackoverflow.com/a/10261848
                    if (!field.IsLiteral || !field.IsInitOnly || field.FieldType != typeof(BoundKeyFunction))
                    {
                        continue;
                    }

                    tempList.Add((BoundKeyFunction)field.GetValue(null));
                }
            }

            tempList.Sort();

            for (var i = 0; i < tempList.Count; i++)
            {
                KeyFunctionsMap.Add(tempList[i], i);
            }
        }

        protected int KeyFunctionID(BoundKeyFunction function)
        {
            return KeyFunctionsMap[function];
        }
    }
}
