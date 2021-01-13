using Robust.Shared.Interfaces.Reflection;
using System;
using System.Collections.Generic;
using System.Reflection;
using Robust.Shared.Serialization;

namespace Robust.Shared.Input
{
    /// <summary>
    ///     A networked identifier for a <see cref="BoundKeyFunction"/>.
    /// </summary>
    [Serializable, NetSerializable]
    public readonly struct KeyFunctionId : IEquatable<KeyFunctionId>
    {
        private readonly int _value;

        public KeyFunctionId(int id)
        {
            _value = id;
        }

        public static explicit operator int(KeyFunctionId funcId)
        {
            return funcId._value;
        }

        public override string ToString()
        {
            return _value.ToString();
        }

        public bool Equals(KeyFunctionId other)
        {
            return _value == other._value;
        }

        public override bool Equals(object? obj)
        {
            return obj is KeyFunctionId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value;
        }

        public static bool operator ==(KeyFunctionId left, KeyFunctionId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(KeyFunctionId left, KeyFunctionId right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    ///     Sets up a mapping of <see cref="BoundKeyFunction"/> to <see cref="KeyFunctionId"/> for network messages.
    /// </summary>
    public class BoundKeyMap
    {
        private readonly IReflectionManager reflectionManager;

        private readonly Dictionary<BoundKeyFunction, KeyFunctionId> KeyFunctionsMap = new();
        private readonly List<BoundKeyFunction> KeyFunctionsList = new();

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

                    KeyFunctionsList.Add((BoundKeyFunction)field.GetValue(null)!);
                }
            }

            KeyFunctionsList.Sort();

            for (var i = 0; i < KeyFunctionsList.Count; i++)
            {
                KeyFunctionsMap.Add(KeyFunctionsList[i], new KeyFunctionId(i));
            }
        }

        public bool FunctionExists(string name)
        {
            return KeyFunctionsMap.ContainsKey(new BoundKeyFunction(name));
        }

        public KeyFunctionId KeyFunctionID(BoundKeyFunction function)
        {
            return KeyFunctionsMap[function];
        }

        public BoundKeyFunction KeyFunctionName(KeyFunctionId function)
        {
            return KeyFunctionsList[(int) function];
        }

        public bool TryGetKeyFunction(KeyFunctionId funcId, out BoundKeyFunction func)
        {
            var list = KeyFunctionsList;
            var index = (int) funcId;

            if (0 > index || index >= list.Count)
            {
                func = default;
                return false;
            }

            func = list[index];
            return true;
        }
    }
}
