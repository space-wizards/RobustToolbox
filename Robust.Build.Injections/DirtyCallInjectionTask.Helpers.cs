using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Robust.Build.Injections
{
    public partial class DirtyCallInjectionTask
    {
        static bool IsDefaultBody(MethodBody methodBody)
        {
            return methodBody.Instructions.Count == 4 && methodBody.Instructions[0].OpCode == OpCodes.Ldarg_0 &&
                   methodBody.Instructions[1].OpCode == OpCodes.Ldarg_1 &&
                   methodBody.Instructions[2].OpCode == OpCodes.Stfld &&
                   methodBody.Instructions[2].Operand is FieldReference &&
                   methodBody.Instructions[3].OpCode == OpCodes.Ret;
        }

        static bool IsComponent(TypeDefinition typeDef)
        {
            if (typeDef.FullName == "Robust.Shared.GameObjects.Component") return true;

            try
            {
                return IsComponent(typeDef.BaseType.Resolve());
            }
            catch (Exception) //resolve fails or basetype is null
            {
                return false;
            }
        }

        static string GetPdbPath(string p)
        {
            var d = Path.GetDirectoryName(p);
            var f = Path.GetFileNameWithoutExtension(p);
            var rv = f + ".pdb";
            if (d != null)
                rv = Path.Combine(d, rv);
            return rv;
        }

        static MethodReference GetEqualsMethodRecursive(ModuleDefinition module, TypeDefinition typeDef)
        {
            var equalsMethod =
                typeDef.Methods.FirstOrDefault(m => m.Name == "Equals" && m.Parameters.Count == 1 && !m.IsStatic);
            if (equalsMethod != null)
            {
                return module.ImportReference(equalsMethod);
            }

            if (typeDef.BaseType == null) return null;

            try
            {
                return GetEqualsMethodRecursive(module, typeDef.BaseType.Resolve());
            }
            catch (Exception) //resolve failed
            {
                return null;
            }
        }
    }
}
