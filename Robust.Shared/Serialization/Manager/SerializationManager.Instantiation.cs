using System;
using System.Collections.Concurrent;
using System.Reflection.Emit;

namespace Robust.Shared.Serialization.Manager
{
    public partial class SerializationManager
    {
        private delegate T InstantiationDelegate<out T>();

        private readonly ConcurrentDictionary<Type, InstantiationDelegate<object>?> _instantiators = new();

        private InstantiationDelegate<object>? GetOrCreateInstantiator(Type type)
        {
            return _instantiators.GetOrAdd(type, static type =>
            {
                var method = new DynamicMethod(
                    "Instantiator",
                    typeof(object),
                    new[] { typeof(object) },
                    true);

                var generator = method.GetILGenerator();

                var constructor = type.GetConstructor(Type.EmptyTypes);

                if (constructor == null)
                {
                    if (!type.IsValueType) return null;
                    generator.DeclareLocal(type);
                    generator.Emit(OpCodes.Ldloca_S, 0);
                    generator.Emit(OpCodes.Initobj, type);
                    generator.Emit(OpCodes.Ldloc_0);
                }
                else
                {
                    generator.Emit(OpCodes.Newobj, constructor);
                }

                if (type.IsValueType)
                    generator.Emit(OpCodes.Box, type);

                generator.Emit(OpCodes.Ret);

                return method.CreateDelegate<InstantiationDelegate<object>>();
            });
        }
    }
}
