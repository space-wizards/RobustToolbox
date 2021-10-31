using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Lidgren.Network;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;
using Robust.Shared.Maths;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Scripting
{
    internal static class ScriptInstanceShared
    {
        public static CSharpParseOptions ParseOptions { get; } =
            new(kind: SourceCodeKind.Script, languageVersion: LanguageVersion.Latest);

        private static readonly Func<Script, bool> _hasReturnValue;
        private static readonly Func<Diagnostic, IReadOnlyList<object?>?> _getDiagnosticArguments;

        public static readonly string[] DefaultImports =
        {
            "System",
            "System.Linq",
            "System.Collections.Generic",
            "Robust.Shared.IoC",
            "Robust.Shared.Maths",
            "Robust.Shared.GameObjects",
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
            }
            else
            {
                _hasReturnValue = (Func<Script, bool>) Delegate.CreateDelegate(typeof(Func<Script, bool>), method);
            }

            // Also internal and we need it.
            var prop = typeof(Diagnostic).GetProperty("Arguments", BindingFlags.Instance | BindingFlags.NonPublic);
            if (prop == null)
            {
                _getDiagnosticArguments = _ => null;
            }
            else
            {
                var moment = prop.GetMethod!;
                _getDiagnosticArguments = moment.CreateDelegate<Func<Diagnostic, IReadOnlyList<object?>?>>();
            }

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

        public static IReadOnlyList<object?>? GetDiagnosticArgs(Diagnostic diag)
        {
            return _getDiagnosticArguments(diag);
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
                if (!ScriptingColorScheme.ColorScheme.TryGetValue(span.ClassificationType, out var color))
                        color = ScriptingColorScheme.ColorScheme[ScriptingColorScheme.Default];

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

        private static IEnumerable<Assembly> GetAutoImportAssemblies(IReflectionManager refl)
        {
            return GetDefaultReferences(refl).Union(
                AssemblyLoadContext.Default.Assemblies.Where(c => c.GetName().Name!.StartsWith("System."))
            );
        }

        public static bool CalcAutoImports(
            IReflectionManager refl,
            IEnumerable<Diagnostic> diags,
            [NotNullWhen(true)] out HashSet<string>? found)
        {
            var missing = new List<string>();
            foreach (var diag in diags.Where(c => c.Id == "CS0103" || c.Id == "CS0246"))
            {
                var args = GetDiagnosticArgs(diag);
                if (args == null)
                {
                    found = null;
                    return false;
                }

                missing.Add((string) args[0]!);
            }

            if (missing.Count == 0)
            {
                found = null;
                return false;
            }

            found = new HashSet<string>();
            var assemblies = ScriptInstanceShared.GetAutoImportAssemblies(refl).ToArray();
            foreach (var m in missing)
            {
                foreach (var assembly in assemblies)
                {
                    foreach (var type in assembly.DefinedTypes)
                    {
                        if (type.IsPublic && type.Name == m)
                        {
                            found.Add(type.Namespace!);
                            goto nextMissing;
                        }
                    }
                }

                nextMissing: ;
            }

            return true;
        }


        public static ScriptOptions GetScriptOptions(IReflectionManager reflectionManager)
        {
            return ScriptOptions.Default
                .AddImports(DefaultImports)
                .AddReferences(GetDefaultReferences(reflectionManager));
        }

        public static string SafeFormat(object obj)
        {
            // Working "around" https://github.com/dotnet/roslyn/issues/51548

            try
            {
                return CSharpObjectFormatter.Instance.FormatObject(obj);
            }
            catch (NotSupportedException)
            {
                return "<CSharpObjectFormatter.FormatObject threw>";
            }
        }
    }
}
