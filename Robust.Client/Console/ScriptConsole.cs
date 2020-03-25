#if SCRIPTING
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lidgren.Network;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;
using Robust.Client.Graphics.Drawing;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.ViewVariables;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

#nullable enable

namespace Robust.Client.Console
{
    internal sealed class ScriptConsole : SS14Window
    {
#pragma warning disable 649
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;
#pragma warning restore 649

        private static readonly CSharpParseOptions _parseOptions =
            new CSharpParseOptions(kind: SourceCodeKind.Script, languageVersion: LanguageVersion.Latest);

        private static readonly Func<Script, bool> _hasReturnValue;

        private static readonly string[] _defaultImports =
        {
            "System",
            "System.Linq",
            "System.Collections.Generic",
            "Robust.Shared.IoC",
            "Robust.Shared.Maths",
            "Robust.Shared.GameObjects",
            "Robust.Shared.Interfaces.GameObjects"
        };

        private readonly OutputPanel _outputPanel;
        private readonly LineEdit _inputBar;

        private readonly StringBuilder _inputBuffer = new StringBuilder();
        private int _linesEntered;

        // Necessary for syntax highlighting.
        private readonly Workspace _highlightWorkspace = new AdhocWorkspace();

        private readonly ScriptGlobals _globals;
        private ScriptState? _state;

        static ScriptConsole()
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

        public ScriptConsole()
        {
            _globals = new ScriptGlobals(this);

            IoCManager.InjectDependencies(this);

            Title = Loc.GetString("Robust C# Interactive");

            Contents.AddChild(new VBoxContainer
            {
                Children =
                {
                    new PanelContainer
                    {
                        PanelOverride = new StyleBoxFlat
                        {
                            BackgroundColor = Color.FromHex("#1E1E1E"),
                            ContentMarginLeftOverride = 4
                        },
                        Children =
                        {
                            (_outputPanel = new OutputPanel
                            {
                                SizeFlagsVertical = SizeFlags.FillExpand,
                            })
                        },
                        SizeFlagsVertical = SizeFlags.FillExpand
                    },
                    (_inputBar = new HistoryLineEdit {PlaceHolder = Loc.GetString("Your C# code here.")})
                }
            });

            _inputBar.OnTextEntered += InputBarOnOnTextEntered;
            CustomMinimumSize = (550, 300);

            _outputPanel.AddText(Loc.GetString(@"Robust C# interactive console."));
            _outputPanel.AddText(">");
        }

        private async void InputBarOnOnTextEntered(LineEdit.LineEditEventArgs obj)
        {
            var code = _inputBar.Text;
            _inputBar.Clear();

            _inputBuffer.AppendLine(code);
            _linesEntered += 1;

            // Remove > or . at the end of the output panel.
            _outputPanel.RemoveEntry(^1);

            var tree = SyntaxFactory.ParseSyntaxTree(SourceText.From(_inputBuffer.ToString()), _parseOptions);

            if (!SyntaxFactory.IsCompleteSubmission(tree))
            {
                if (_linesEntered == 1)
                {
                    _outputPanel.AddText($"> {code}");
                }
                else
                {
                    _outputPanel.AddText($". {code}");
                }
                _outputPanel.AddText(".");
                return;
            }

            code = _inputBuffer.ToString().Trim();

            // Remove echo of partial submission from the output panel.
            for (var i = 1; i < _linesEntered; i++)
            {
                _outputPanel.RemoveEntry(^1);
            }

            _inputBuffer.Clear();
            _linesEntered = 0;

            Script newScript;

            if (_state != null)
            {
                newScript = _state.Script.ContinueWith(code);
            }
            else
            {
                var options = GetScriptOptions();
                newScript = CSharpScript.Create(code, options, typeof(IScriptGlobals));
            }

            // Compile ahead of time so that we can do syntax highlighting correctly for the echo.
            newScript.Compile();

            // Echo entered script.
            var echoMessage = new FormattedMessage();
            echoMessage.PushColor(Color.FromHex("#D4D4D4"));
            echoMessage.AddText("> ");
            AddWithSyntaxHighlighting(newScript, echoMessage, code, _highlightWorkspace);

            _outputPanel.AddMessage(echoMessage);

            try
            {
                if (_state != null)
                {
                    _state = await newScript.RunFromAsync(_state, _ => true);
                }
                else
                {
                    _state = await newScript.RunAsync(_globals);
                }
            }
            catch (CompilationErrorException e)
            {
                var msg = new FormattedMessage();

                msg.PushColor(Color.Crimson);

                foreach (var diagnostic in e.Diagnostics)
                {
                    msg.AddText(diagnostic.ToString());
                    msg.AddText("\n");
                }

                _outputPanel.AddMessage(msg);
                _outputPanel.AddText(">");
                return;
            }

            if (_state.Exception != null)
            {
                var msg = new FormattedMessage();
                msg.PushColor(Color.Crimson);
                msg.AddText(CSharpObjectFormatter.Instance.FormatException(_state.Exception));
                _outputPanel.AddMessage(msg);
            }
            else if (_hasReturnValue(newScript))
            {
                var msg = new FormattedMessage();
                msg.AddText(CSharpObjectFormatter.Instance.FormatObject(_state.ReturnValue));
                _outputPanel.AddMessage(msg);
            }

            _outputPanel.AddText(">");
        }

        protected override void Opened()
        {
            _inputBar.GrabKeyboardFocus();
        }

        private ScriptOptions GetScriptOptions()
        {
            return ScriptOptions.Default
                .AddImports(_defaultImports)
                .AddReferences(GetDefaultReferences());
        }

        private static void AddWithSyntaxHighlighting(Script script, FormattedMessage msg, string code,
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

        private IEnumerable<Assembly> GetDefaultReferences()
        {
            var list = new List<Assembly>();

            list.AddRange(_reflectionManager.Assemblies);
            list.Add(typeof(YamlDocument).Assembly); // YamlDotNet
            list.Add(typeof(NetPeer).Assembly); // Lidgren
            list.Add(typeof(Vector2).Assembly); // Robust.Shared.Maths

            return list;
        }

        private sealed class ScriptGlobals : IScriptGlobals
        {
            private readonly ScriptConsole _owner;

            [field: Dependency] public IEntityManager ent { get; } = default!;
            [field: Dependency] public IComponentManager comp { get; } = default!;
            [field: Dependency] public IViewVariablesManager vvm { get; } = default!;

            public ScriptGlobals(ScriptConsole owner)
            {
                IoCManager.InjectDependencies(this);

                _owner = owner;
            }

            public void vv(object a)
            {
                vvm.OpenVV(a);
            }

            public T res<T>()
            {
                return IoCManager.Resolve<T>();
            }

            public void write(object toString)
            {
                _owner._outputPanel.AddText(toString?.ToString() ?? "");
            }

            public void show(object obj)
            {
                write(CSharpObjectFormatter.Instance.FormatObject(obj));
            }
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [PublicAPI]
    public interface IScriptGlobals
    {
        public IEntityManager ent { get; }
        public IComponentManager comp { get; }
        public IViewVariablesManager vvm { get; }

        public void vv(object a);
        public T res<T>();
        public void write(object toString);
        public void show(object obj);
    }
}
#endif
