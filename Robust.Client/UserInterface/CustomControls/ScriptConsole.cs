using System;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Client.Console;
using Robust.Client.Editor.Interface;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Robust.Client.UserInterface.CustomControls
{
    internal abstract class ScriptConsole : EditorPanel, IConsoleTab
    {
        protected OutputPanel OutputPanel { get; }
        protected HistoryLineEdit InputBar { get; }
        protected Button RunButton { get; }
        protected Completions Suggestions { get; }

        public event Action<IConsoleTab>? Focused;

        protected ScriptConsole()
        {
            AddChild(new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Children =
                {
                    (OutputPanel = new OutputPanel
                    {
                        VerticalExpand = true,
                        Margin = new Thickness(4)
                    }),
                    new BoxContainer
                    {
                        Orientation = LayoutOrientation.Horizontal,
                        Children =
                        {
                            (InputBar = new HistoryLineEdit
                            {
                                HorizontalExpand = true,
                                PlaceHolder = "Your C# code here."
                            }),
                            (RunButton = new Button {Text = "Run"})
                        }
                    },
                }
            });

            Suggestions = new Completions(InputBar);
            InputBar.OnTabComplete += _ => Complete();
            InputBar.OnTextChanged += _ => Suggestions.TextChanged();
            InputBar.OnTextEntered += _ => Run();
            RunButton.OnPressed += _ => Run();

            InputBar.OnFocusEnter += _ =>
            {
                Focused?.Invoke(this);
            };
        }

        protected abstract void Complete();

        protected abstract void Run();

        LineEdit IConsoleTab.CommandBar => InputBar;

        protected internal override void TabFocused()
        {
            InputBar.GrabKeyboardFocus();
        }
    }
}
