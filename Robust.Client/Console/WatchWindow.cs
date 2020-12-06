#if CLIENT_SCRIPTING

using System;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Scripting;
using Robust.Shared.Timing;

namespace Robust.Client.Console
{
    public class WatchWindow : SS14Window
    {
        private readonly IReflectionManager _reflectionManager;

        protected override Vector2? CustomSize => (300, 300);

        private readonly VBoxContainer _watchesVBox;
        private readonly LineEdit _addWatchEdit;
        private readonly Button _addWatchButton;


        public WatchWindow()
        {
            _reflectionManager = IoCManager.Resolve<IReflectionManager>();

            ScriptInstanceShared.InitDummy();

            Title = Loc.GetString("Watch Window");

            var mainVBox = new VBoxContainer
            {
                CustomMinimumSize = (500, 300),
                Children =
                {
                    (_watchesVBox = new VBoxContainer
                    {
                        SizeFlagsVertical = SizeFlags.FillExpand
                    }),
                    new HBoxContainer
                    {
                        Children =
                        {
                            (_addWatchEdit = new HistoryLineEdit
                            {
                                SizeFlagsHorizontal = SizeFlags.FillExpand,
                                PlaceHolder = Loc.GetString("Add watch (C# interactive)")
                            }),
                            (_addWatchButton = new Button
                            {
                                Text = Loc.GetString("Add")
                            })
                        }
                    }
                },
            };

            _addWatchButton.OnPressed += _ => AddWatch();
            _addWatchEdit.OnTextEntered += _ => AddWatch();

            Contents.AddChild(mainVBox);
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

                AddChild(new HBoxContainer
                {
                    Children =
                    {
                        (_outputLabel = new Label
                        {
                            SizeFlagsHorizontal = SizeFlags.FillExpand,
                            ClipText = true
                        }),
                        (delButton = new Button
                        {
                            Text = Loc.GetString("Remove")
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
                AddChild(new HBoxContainer
                {
                    Children =
                    {
                        new Label
                        {
                            Text = message,
                            ClipText = true,
                            SizeFlagsHorizontal = SizeFlags.FillExpand
                        },
                        (delButton = new Button {Text = Loc.GetString("Remove")})
                    }
                });

                delButton.OnPressed += _ => Orphan();
            }
        }
    }
}

#endif
