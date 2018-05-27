using SS14.Shared.Interfaces.Reflection;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SS14.Shared.Input
{
    public class BoundKeyMap
    {
        private readonly IReflectionManager reflectionManager;

        private readonly Dictionary<BoundKeyFunction, int> KeyFunctionsMap = new Dictionary<BoundKeyFunction, int>();
        private readonly List<BoundKeyFunction> KeyFunctionsList = new List<BoundKeyFunction>();

        public BoundKeyMap(IReflectionManager reflectionManager)
        {
            this.reflectionManager = reflectionManager;
        }

        public void PopulateKeyFunctionsMap()
        {
            if (KeyFunctionsMap.Count != 0)
            {
                throw new InvalidOperationException("Cannot run this method twice.");
            }

            foreach (var type in reflectionManager.FindTypesWithAttribute<KeyFunctionsAttribute>())
            {
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
                {
                    // This check makes sure we ONLY get readonly fields with the type we want.
                    if (field.IsLiteral || !field.IsInitOnly || field.FieldType != typeof(BoundKeyFunction))
                    {
                        continue;
                    }

                    KeyFunctionsList.Add((BoundKeyFunction)field.GetValue(null));
                }
            }

            KeyFunctionsList.Sort();

            for (var i = 0; i < KeyFunctionsList.Count; i++)
            {
                KeyFunctionsMap.Add(KeyFunctionsList[i], i);
            }
        }

        public bool FunctionExists(string name)
        {
            return KeyFunctionsMap.ContainsKey(new BoundKeyFunction(name));
        }

        public int KeyFunctionID(BoundKeyFunction function)
        {
            return KeyFunctionsMap[function];
        }

        public BoundKeyFunction KeyFunctionName(int function)
        {
            return KeyFunctionsList[function];
        }
    }
}
