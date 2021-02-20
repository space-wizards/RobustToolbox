using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.CustomControls
{
    internal abstract class ScriptConsole : SS14Window
    {
        protected OutputPanel OutputPanel { get; }
        protected HistoryLineEdit InputBar { get; }
        protected Button RunButton { get; }

        protected ScriptConsole()
        {
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
                            (OutputPanel = new OutputPanel())
                        },
                        VerticalExpand = true,
                    },
                    new HBoxContainer
                    {
                        Children =
                        {
                            (InputBar = new HistoryLineEdit
                            {
                                HorizontalExpand = true,
                                PlaceHolder = Loc.GetString("Your C# code here.")
                            }),
                            (RunButton = new Button {Text = Loc.GetString("Run")})
                        }
                    },
                }
            });

            InputBar.OnTextEntered += _ => Run();
            RunButton.OnPressed += _ => Run();
            CustomMinimumSize = (550, 300);
        }

        protected abstract void Run();

        protected override void Opened()
        {
            InputBar.GrabKeyboardFocus();
        }
    }
}
