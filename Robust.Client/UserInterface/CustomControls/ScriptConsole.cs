using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Client.Console;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Robust.Client.UserInterface.CustomControls
{
    internal abstract class ScriptConsole : SS14Window
    {
        protected OutputPanel OutputPanel { get; }
        protected HistoryLineEdit InputBar { get; }
        protected Button RunButton { get; }
        protected Completions Suggestions { get; }

        protected ScriptConsole()
        {
            Contents.AddChild(new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
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
                            (OutputPanel = new OutputPanel())
                        },
                        VerticalExpand = true,
                    },
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
            MinSize = (550, 300);
        }

        protected abstract void Complete();

        protected abstract void Run();

        protected override void Opened()
        {
            InputBar.GrabKeyboardFocus();
        }
    }
}
