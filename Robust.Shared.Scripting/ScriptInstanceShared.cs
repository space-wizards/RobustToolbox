using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Lidgren.Network;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Scripting
{
    internal static class ScriptInstanceShared
    {
        public static CSharpParseOptions ParseOptions { get; } =
            new(kind: SourceCodeKind.Script, languageVersion: LanguageVersion.Latest);

        private static readonly Func<Script, bool> _hasReturnValue;

        private static readonly string[] _defaultImports =
        {
            "System",
            "System.Linq",
            "System.Collections.Generic",
            "Robust.Shared.IoC",
            "Robust.Shared.Maths",
            "Robust.Shared.GameObjects",
            "Robust.Shared.Interfaces.GameObjects",
            "Robust.Shared.Map",
            "Robust.Shared.Prototypes"
        };

        static ScriptInstanceShared()
        {
            // This is the (internal) method that csi seems to use.
            // Because it is internal and I can't find an alternative, reflection it is.
            // TODO: Find a way that doesn't need me to reflect into Roslyn internals.
            var method = typeof(Script).GetMethod("HasReturnValue", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                // Fallback path in case they remove that.
                // The method literally has a // TODO: remove
                _hasReturnValue = _ => true;
                return;
            }

            _hasReturnValue = (Func<Script, bool>) Delegate.CreateDelegate(typeof(Func<Script, bool>), method);

            // Run this async so that Roslyn can "warm up" in another thread while you're typing in your first line,
            // so the hang when you hit enter is less bad.
            Task.Run(async () =>
            {
                const string code =
                    "var x = 5 + 5; var y = (object) \"foobar\"; void Foo(object a) { } Foo(y); Foo(x)";

                var script = await CSharpScript.RunAsync(code);
                var msg = new FormattedMessage();
                // Even run the syntax highlighter!
                AddWithSyntaxHighlighting(script.Script, msg, code, new AdhocWorkspace());
            });
        }

        /// <summary>
        ///     Does nothing, but will invoke the static constructor so Roslyn can warm up.
        /// </summary>
        public static void InitDummy()
        {
            // Nada.
        }

        public static bool HasReturnValue(Script script)
        {
            return _hasReturnValue(script);
        }

        public static void AddWithSyntaxHighlighting(Script script, FormattedMessage msg, string code,
            Workspace workspace)
        {
            var compilation = script.GetCompilation();
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.First());

            var classified = Classifier.GetClassifiedSpans(model, TextSpan.FromBounds(0, code.Length), workspace);

            var current = 0;
            foreach (var span in classified)
            {
                var start = span.TextSpan.Start;
                if (start > current)
                {
                    msg.AddText(code[current..start]);
                }

                if (current > start)
                {
                    continue;
                }

                // TODO: there are probably issues with multiple classifications overlapping the same text here.
                // Too lazy to fix.
                var src = code[span.TextSpan.Start..span.TextSpan.End];
                var color = span.ClassificationType switch
                {
                    ClassificationTypeNames.Comment => Color.FromHex("#57A64A"),
                    ClassificationTypeNames.NumericLiteral => Color.FromHex("#b5cea8"),
                    ClassificationTypeNames.StringLiteral => Color.FromHex("#D69D85"),
                    ClassificationTypeNames.Keyword => Color.FromHex("#569CD6"),
                    ClassificationTypeNames.StaticSymbol => Color.FromHex("#4EC9B0"),
                    ClassificationTypeNames.ClassName => Color.FromHex("#4EC9B0"),
                    ClassificationTypeNames.StructName => Color.FromHex("#4EC9B0"),
                    ClassificationTypeNames.InterfaceName => Color.FromHex("#B8D7A3"),
                    ClassificationTypeNames.EnumName => Color.FromHex("#B8D7A3"),
                    _ => Color.FromHex("#D4D4D4")
                };

                msg.PushColor(color);
                msg.AddText(src);
                msg.Pop();
                current = span.TextSpan.End;
            }

            msg.AddText(code[current..]);
        }

        private static IEnumerable<Assembly> GetDefaultReferences(IReflectionManager reflectionManager)
        {
            var list = new List<Assembly>();

            list.AddRange(reflectionManager.Assemblies);
            list.Add(typeof(YamlDocument).Assembly); // YamlDotNet
            list.Add(typeof(NetPeer).Assembly); // Lidgren
            list.Add(typeof(Vector2).Assembly); // Robust.Shared.Maths

            return list;
        }

        public static ScriptOptions GetScriptOptions(IReflectionManager reflectionManager)
        {
            return ScriptOptions.Default
                .AddImports(_defaultImports)
                .AddReferences(GetDefaultReferences(reflectionManager));
        }
    }
}
