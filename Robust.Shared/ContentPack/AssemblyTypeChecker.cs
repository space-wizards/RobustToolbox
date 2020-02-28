using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using Robust.Shared.Log;

namespace Robust.Shared.ContentPack
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
            "Robust.Shared.",
            "Robust.Client.",
            "Robust.Server.",

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
            "System.Boolean",
            "System.String",
            "System.Char",
            "System.Byte",
            "System.SByte",
            "System.UInt16",
            "System.Int16",
            "System.UInt32",
            "System.Int32",
            "System.UInt64",
            "System.Int64",
            "System.Single",
            "System.Double",
            "System.Decimal",
            "System.Void",
            "System.Enum",

            // Common System things.
            "System.Collections.Generic",
            "System.Collections.IEnumerator",
            "System.Console",
            "System.Math",
            "System.ArgumentNullException",
            "System.Attribute",
            "System.AttributeUsageAttribute",
            "System.Convert",
            "System.Delegate", // Pls be good.
            "System.DivideByZeroException",
            "System.EventArgs",
            "System.Exception",
            "System.FlagsAttribute",
            "System.FormatException",
            "System.IndexOutOfRangeException",
            "System.InvalidCastException",
            "System.NotSupportedException",
            "System.NullReferenceException",
            "System.SerializableAttribute",
            "System.InvalidOperationException",
            "System.ArgumentException",
            "System.NotImplementedException",
            "System.Tuple",
            "System.ValueTuple",
            "System.Action",
            "System.Func",
            "System.EventHandler",
            "System.IDisposable",
            "System.ICloneable",
            "System.IComparable",
            "System.IEquatable",
            "System.ParamArrayAttribute",
            "System.Threading.Interlocked",

            // SFML stuff.
            "SFML.Graphics.Color",
            "SFML.System.Vector2",
            "SFML.System.Vector2i",
            "SFML.System.Vector2u",
            "SFML.System.Vector3",
            "SFML.System.Vector3i",
            "SFML.System.Vector3u",

            // YamlDotNet representation (prototype parsing)
            "YamlDotNet.RepresentationModel.YamlNode",
            "YamlDotNet.RepresentationModel.YamlScalarNode",
            "YamlDotNet.RepresentationModel.YamlMappingNode",
            "YamlDotNet.RepresentationModel.YamlSequenceNode",

            // No, I did not write this out by hand.
            "OpenTK.Graphics.Color4",
            "OpenTK.BezierCurve",
            "OpenTK.BezierCurveCubic",
            "OpenTK.BezierCurveQuadric",
            "OpenTK.Box2",
            "OpenTK.Box2d",
            "OpenTK.Half",
            "OpenTK.MathHelper",
            "OpenTK.Matrix2",
            "OpenTK.Matrix2d",
            "OpenTK.Matrix2x3",
            "OpenTK.Matrix2x3d",
            "OpenTK.Matrix2x4",
            "OpenTK.Matrix2x4d",
            "OpenTK.Matrix3",
            "OpenTK.Matrix3d",
            "OpenTK.Matrix3x2",
            "OpenTK.Matrix3x2d",
            "OpenTK.Matrix3x4",
            "OpenTK.Matrix3x4d",
            "OpenTK.Matrix4",
            "OpenTK.Matrix4d",
            "OpenTK.Matrix4x2",
            "OpenTK.Matrix4x2d",
            "OpenTK.Matrix4x3",
            "OpenTK.Matrix4x3d",
            "OpenTK.Point",
            "OpenTK.Quaternion",
            "OpenTK.Quaterniond",
            "OpenTK.Rectangle",
            "OpenTK.Size",
            "OpenTK.Vector2",
            "OpenTK.Vector2d",
            "OpenTK.Vector2h",
            "OpenTK.Vector3",
            "OpenTK.Vector3d",
            "OpenTK.Vector3h",
            "OpenTK.Vector4",
            "OpenTK.Vector4d",
            "OpenTK.Vector4h",
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
        public static bool CheckAssembly(Stream assembly)
        {
            if (WouldNoOp)
            {
                // This method is a no-op in this case so don't bother
                return true;
            }

            var asmDef = AssemblyDefinition.ReadAssembly(assembly);

            if (DumpTypes)
                AnalyzeTypes(asmDef.MainModule.GetTypeReferences());

            if (DisableTypeCheck)
                return true;

            foreach (var typeRef in asmDef.MainModule.GetTypeReferences())
            {
                // Assemblies are guilty until proven innocent in a court of law.
                var safe = false;
                foreach (var typeName in _typeWhiteList)
                {
                    if (typeRef.FullName.StartsWith(typeName))
                        safe = true;
                }

                foreach (var typeName in _typeBlackList)
                {
                    if (typeRef.FullName.StartsWith(typeName))
                        safe = false;
                }

                if (safe)
                    continue;

                Logger.ErrorS("res.typecheck", $"Cannot load {asmDef.MainModule.Name}, {typeRef.FullName} is not whitelisted.");
                return false;
            }

            return true;
        }

        private static bool WouldNoOp => !DumpTypes && DisableTypeCheck;

        public static bool CheckAssembly(string diskPath)
        {
            if (WouldNoOp)
            {
                // This method is a no-op in this case so don't bother
                return true;
            }

            using var file = File.OpenRead(diskPath);

            return CheckAssembly(file);
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
                Logger.DebugS("res.typecheck", $"RefType: [{result}] {typeRef.FullName}");
            }
        }
    }
}
