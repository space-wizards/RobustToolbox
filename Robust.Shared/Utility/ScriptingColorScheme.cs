using System.Collections.Generic;
using Robust.Shared.Maths;

namespace Robust.Shared.Utility
{
    public static class ScriptingColorScheme
    {
        public const string ClassName = "class name";
        public const string Comment = "comment";
        public const string EnumName = "enum name";
        public const string FieldName = "field name";
        public const string InterfaceName = "interface name";
        public const string Keyword = "keyword";
        public const string MethodName = "method name";
        public const string NamespaceName = "namespace name";
        public const string NumericLiteral = "number";
        public const string PropertyName = "property name";
        public const string StaticSymbol = "static symbol";
        public const string StringLiteral = "string";
        public const string StructName = "struct name";
        public const string Default = "default";

        public static readonly Dictionary<string, Color> ColorScheme = new()
        {
            {ClassName, Color.FromHex("#4EC9B0")},
            {Comment, Color.FromHex("#57A64A")},
            {EnumName, Color.FromHex("#B8D7A3")},
            {FieldName, Color.FromHex("#C86E11")},
            {InterfaceName, Color.FromHex("#B8D7A3")},
            {Keyword, Color.FromHex("#569CD6")},
            {MethodName, Color.FromHex("#11A3C8")},
            {NamespaceName, Color.FromHex("#C8A611")},
            {NumericLiteral, Color.FromHex("#b5cea8")},
            {PropertyName, Color.FromHex("#11C89D")},
            {StaticSymbol, Color.FromHex("#4EC9B0")},
            {StringLiteral, Color.FromHex("#D69D85")},
            {StructName, Color.FromHex("#4EC9B0")},
            {Default, Color.FromHex("#D4D4D4")},
        };
    }
}
