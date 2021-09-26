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

                if (type.IsValueType)
                {
                    generator.DeclareLocal(type);
                    generator.DeclareLocal(typeof(object));

                    generator.Emit(OpCodes.Ldloca_S, 0);

                    generator.Emit(OpCodes.Initobj, type);

                    generator.Emit(OpCodes.Ldloc_0);
                    generator.Emit(OpCodes.Box, type);
                    generator.Emit(OpCodes.Stloc_1);

                    generator.Emit(OpCodes.Ldloc_1);
                    generator.Emit(OpCodes.Ret);
                }
                else
                {
                    generator.DeclareLocal(typeof(object));

                    var constructor = type.GetConstructor(Type.EmptyTypes);

                    if (constructor == null)
                    {
                        return null;
                    }

                    generator.Emit(OpCodes.Newobj, constructor);
                    generator.Emit(OpCodes.Stloc_0);

                    generator.Emit(OpCodes.Ldloc_0);
                    generator.Emit(OpCodes.Ret);
                }

                return method.CreateDelegate<InstantiationDelegate<object>>();
            });
        }
    }
}
