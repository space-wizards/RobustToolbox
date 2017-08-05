using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using SS14.Shared.Log;

namespace SS14.Shared.ContentPack
{
    /// <summary>
    ///     Manages the type white/black list of types and namespaces, and verifies assemblies against them.
    /// </summary>
    internal static class AssemblyTypeChecker
    {
        /// <summary>
        ///     Namespaces/Types that are explicitly allowed.
        /// </summary>
        private static readonly List<string> _typeWhiteList = new List<string>
        {
            // base types for making a dll
            "System.Runtime.CompilerServices.CompilationRelaxationsAttribute",
            "System.Runtime.CompilerServices.RuntimeCompatibilityAttribute",
            "System.Diagnostics.DebuggableAttribute",
            "System.Diagnostics.DebuggableAttribute/DebuggingModes",
            "System.Reflection.AssemblyTitleAttribute",
            "System.Reflection.AssemblyDescriptionAttribute",
            "System.Reflection.AssemblyConfigurationAttribute",
            "System.Reflection.AssemblyCompanyAttribute",
            "System.Reflection.AssemblyProductAttribute",
            "System.Reflection.AssemblyCopyrightAttribute",
            "System.Reflection.AssemblyTrademarkAttribute",
            "System.Runtime.InteropServices.ComVisibleAttribute",
            "System.Runtime.InteropServices.GuidAttribute",
            "System.Reflection.AssemblyFileVersionAttribute",
            "System.Runtime.Versioning.TargetFrameworkAttribute",
            "System.Runtime.CompilerServices.CompilerGeneratedAttribute",
            "System.Diagnostics.DebuggerBrowsableState",
            "System.Diagnostics.DebuggerBrowsableAttribute",

            // engine assemblies
            "SS14.Shared.",
            "SS14.Client.",
            "SS14.Server.",

            // Content assemblies
            "Content.Shared.",
            "Content.Client.",
            "Content.server.",

            // base type assemblies
            "System.Object",
            "System.Nullable`1",
            "System.ValueType",
            "System.Array",

            // Primitives
            "System.String",
            "System.Byte",
            "System.SByte",
            "System.UInt16",
            "System.Int16",
            "System.UInt32",
            "System.Int32",
            "System.UInt64",
            "System.Int64",

            // Common System things.
            "System.Collections.Generic",
            "System.Console",
        };

        /// <summary>
        ///     Namespaces/Types that are restricted. This overrides the white list.
        /// </summary>
        private static readonly List<string> _typeBlackList = new List<string>
        {
            // IL stuff
            "System.Linq.Expressions",
            "System.CodeDom",
            //"System.Reflection", // leave this in grey so that we can allow some things inside of this namespace

            // IO Stuff
            "System.IO",
            "System.Xml",

            // Sockets Stuff
            "System.Net"
        };

        /// <summary>
        ///     Completely disables type checking, allowing everything.
        /// </summary>
        public static bool DisableTypeCheck { get; set; } = false;

        /// <summary>
        ///     Dump the assembly types into the log.
        /// </summary>
        public static bool DumpTypes { get; set; } = true;

        /// <summary>
        ///     Check the assembly for any illegal types. Any types not on the white list
        ///     will cause the assembly to be rejected.
        /// </summary>
        /// <param name="assembly">Assembly to load.</param>
        /// <returns></returns>
        public static bool CheckAssembly(byte[] assembly)
        {
            using (var asmDefStream = new MemoryStream(assembly))
            {
                var asmDef = AssemblyDefinition.ReadAssembly(asmDefStream);

                if (DumpTypes)
                    AnalyzeTypes(asmDef.MainModule.GetTypeReferences());

                if (DisableTypeCheck)
                    return true;

                foreach (var typeRef in asmDef.MainModule.GetTypeReferences())
                {
                    // Assemblies are guilty until proven innocent in a court of law.
                    var safe = false;
                    foreach (var typeName in _typeWhiteList)
                        if (typeRef.FullName.StartsWith(typeName))
                            safe = true;

                    foreach (var typeName in _typeBlackList)
                        if (typeRef.FullName.StartsWith(typeName))
                            safe = false;

                    if (safe)
                        continue;

                    Logger.Error($"[RES] Cannot load {asmDef.MainModule.Name}, {typeRef.FullName} is not whitelisted.");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Runs an enumeration of types through the white/black lists and prints results to log.
        /// </summary>
        /// <param name="types">Types to check.</param>
        private static void AnalyzeTypes(IEnumerable<TypeReference> types)
        {
            foreach (var typeRef in types)
            {
                var result = 'G';
                foreach (var typeName in _typeWhiteList)
                {
                    if (!typeRef.FullName.StartsWith(typeName))
                        continue;

                    result = 'W';
                    break;
                }

                foreach (var typeName in _typeBlackList)
                {
                    if (!typeRef.FullName.StartsWith(typeName))
                        continue;

                    result = 'B';
                    break;
                }
                Logger.Debug($"[RES] RefType: [{result}] {typeRef.FullName}");
            }
        }
    }
}
