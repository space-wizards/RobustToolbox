#if CLIENT_SCRIPTING

using System;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Scripting;
using Robust.Shared.Timing;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Robust.Client.Console
{
    public class WatchWindow : DefaultWindow
    {
        private readonly IReflectionManager _reflectionManager;

        private readonly BoxContainer _watchesVBox;
        private readonly LineEdit _addWatchEdit;
        private readonly Button _addWatchButton;


        public WatchWindow()
        {
            _reflectionManager = IoCManager.Resolve<IReflectionManager>();

            ScriptInstanceShared.InitDummy();

            Title = "Watch Window";

            var mainVBox = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                MinSize = (500, 300),
                Children =
                {
                    (_watchesVBox = new BoxContainer
                    {
                        Orientation = LayoutOrientation.Vertical,
                        VerticalExpand = true
                    }),
                    new BoxContainer
                    {
                        Orientation = LayoutOrientation.Horizontal,
                        Children =
                        {
                            (_addWatchEdit = new HistoryLineEdit
                            {
                                HorizontalExpand = true,
                                PlaceHolder = "Add watch (C# interactive)"
                            }),
                            (_addWatchButton = new Button
                            {
                                Text = "Add"
                            })
                        }
                    }
                },
            };

            _addWatchButton.OnPressed += _ => AddWatch();
            _addWatchEdit.OnTextEntered += _ => AddWatch();

            Contents.AddChild(mainVBox);

            SetSize = (300, 300);
        }

        private void AddWatch()
        {
            var code = _addWatchEdit.Text;
            if (string.IsNullOrWhiteSpace(code))
            {
                return;
            }

            var options = ScriptInstanceShared.GetScriptOptions(_reflectionManager);
            var script = CSharpScript.Create(code, options, typeof(ScriptGlobalsShared));

            ScriptRunner<object> @delegate;
            try
            {
                @delegate = script.CreateDelegate();
            }
            catch (CompilationErrorException compilationError)
            {
                _watchesVBox.AddChild(new CompilationErrorControl(string.Join('\n', compilationError.Diagnostics)));
                return;
            }

            var control = new WatchControl(@delegate);

            _watchesVBox.AddChild(control);
            _addWatchEdit.Clear();
        }

        private sealed class WatchControl : Control
        {
            private readonly ScriptRunner<object> _runner;
            private readonly ScriptGlobalsImpl _globals = new();
            private readonly Label _outputLabel;

            public WatchControl(ScriptRunner<object> runner)
            {
                Button delButton;
                _runner = runner;

                AddChild(new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    Children =
                    {
                        (_outputLabel = new Label
                        {
                            HorizontalExpand = true,
                            ClipText = true
                        }),
                        (delButton = new Button
                        {
                            Text = "Remove"
                        }),
                    }
                });

                delButton.OnPressed += _ => Orphan();
            }

            protected override async void FrameUpdate(FrameEventArgs args)
            {
                base.FrameUpdate(args);

                object obj;
                try
                {
                    obj = await _runner(_globals);
                }
                catch (Exception e)
                {
                    _outputLabel.Text = CSharpObjectFormatter.Instance.FormatException(e);
                    return;
                }

                _outputLabel.Text = CSharpObjectFormatter.Instance.FormatObject(obj);
            }
        }

        private sealed class ScriptGlobalsImpl : ScriptGlobalsShared
        {
            public ScriptGlobalsImpl()
            {
                IoCManager.InjectDependencies(this);
            }

            protected override void WriteSyntax(object toString)
            {
                // No-op: nothing to write to.
            }

            public override void write(object toString)
            {
                // No-op: nothing to write to.
            }

            public override void show(object obj)
            {
                // No-op: nothing to write to.
            }
        }

        private sealed class CompilationErrorControl : Control
        {
            public CompilationErrorControl(string message)
            {
                Button delButton;
                AddChild(new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    Children =
                    {
                        new Label
                        {
                            Text = message,
                            ClipText = true,
                            HorizontalExpand = true
                        },
                        (delButton = new Button {Text = "Remove"})
                    }
                });

                delButton.OnPressed += _ => Orphan();
            }
        }
    }
}

#endif
