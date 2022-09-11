using System;
using System.Linq;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.Console.Commands;

internal sealed class UITestControl : Control
{
    private readonly TabContainer _tabContainer;

    public UITestControl()
    {
        _tabContainer = new TabContainer();
        AddChild(_tabContainer);
        var scroll = new ScrollContainer();
        _tabContainer.AddChild(scroll);
        //scroll.SetAnchorAndMarginPreset(Control.LayoutPreset.Wide);
        var vBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical
        };
        scroll.AddChild(vBox);

        var progressBar = new ProgressBar { MaxValue = 10, Value = 5 };
        vBox.AddChild(progressBar);

        var optionButton = new OptionButton();
        optionButton.AddItem("Honk");
        optionButton.AddItem("Foo");
        optionButton.AddItem("Bar");
        optionButton.AddItem("Baz");
        optionButton.OnItemSelected += eventArgs => optionButton.SelectId(eventArgs.Id);
        vBox.AddChild(optionButton);

        var tree = new Tree { VerticalExpand = true };
        var root = tree.CreateItem();
        root.Text = "Honk!";
        var child = tree.CreateItem();
        child.Text = "Foo";
        for (var i = 0; i < 20; i++)
        {
            child = tree.CreateItem();
            child.Text = $"Bar {i}";
        }

        vBox.AddChild(tree);

        var rich = new RichTextLabel();
        var message = new FormattedMessage();
        message.AddText("Foo\n");
        message.PushColor(Color.Red);
        message.AddText("Bar");
        message.Pop();
        rich.SetMessage(message);
        vBox.AddChild(rich);

        var itemList = new ItemList();
        _tabContainer.AddChild(itemList);
        for (var i = 0; i < 10; i++)
        {
            itemList.AddItem(i.ToString());
        }

        var grid = new GridContainer { Columns = 3 };
        _tabContainer.AddChild(grid);
        for (var y = 0; y < 3; y++)
        {
            for (var x = 0; x < 3; x++)
            {
                grid.AddChild(new Button
                {
                    MinSize = (50, 50),
                    Text = $"{x}, {y}"
                });
            }
        }

        var group = new ButtonGroup();
        var vBoxRadioButtons = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical
        };
        for (var i = 0; i < 10; i++)
        {
            vBoxRadioButtons.AddChild(new Button
            {
                Text = i.ToString(),
                Group = group
            });

            // ftftftftftftft
        }

        _tabContainer.AddChild(vBoxRadioButtons);

        TabContainer.SetTabTitle(vBoxRadioButtons, "Radio buttons!!");

        _tabContainer.AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Name = "Slider",
            Children =
            {
                new Slider()
            }
        });

        _tabContainer.AddChild(new SplitContainer
        {
            Orientation = SplitContainer.SplitOrientation.Horizontal,
            Children =
            {
                new PanelContainer
                {
                    PanelOverride = new StyleBoxFlat { BackgroundColor = Color.Red },
                    Children =
                    {
                        new Label { Text = "FOOBARBAZ" },
                    }
                },
                new PanelContainer
                {
                    PanelOverride = new StyleBoxFlat { BackgroundColor = Color.Blue },
                    Children =
                    {
                        new Label { Text = "FOOBARBAZ" },
                    }
                },
            }
        });
    }

    public void SelectTab(Tab tab)
    {
        _tabContainer.CurrentTab = (int)tab;
    }

    public enum Tab : byte
    {
        Untitled1 = 0,
        Untitled2 = 1,
        Untitled3 = 2,
        RadioButtons = 3,
        Slider = 4,
        Untitled4 = 5,
    }
}

internal sealed class UITestCommand : IConsoleCommand
{
    public string Command => "uitest";
    public string Description => "Open a dummy UI testing window";
    public string Help => "uitest";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var window = new DefaultWindow { MinSize = (500, 400) };
        window.Contents.AddChild(new UITestControl());

        window.OpenCentered();
    }
}

internal sealed class UITest2Command : IConsoleCommand
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IUserInterfaceManager _uiMgr = default!;

    public string Command => "uitest2";
    public string Description => Loc.GetString("cmd-uitest2-desc");
    public string Help => Loc.GetString("cmd-uitest2-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length > 1)
        {
            shell.WriteError(Loc.GetString("cmd-uitest2-error-args"));
            return;
        }

        var control = new UITestControl();

        if (args.Length == 1)
        {
            if (!Enum.TryParse(args[0], out UITestControl.Tab tab))
            {
                shell.WriteError(Loc.GetString("cmd-uitest2-error-tab", ("value", args[0])));
                return;
            }

            control.SelectTab(tab);
        }

        var window = _clyde.CreateWindow(new WindowCreateParameters
        {
            Title = Loc.GetString("cmd-uitest2-title"),
        });

        var root = _uiMgr.CreateWindowRoot(window);
        window.DisposeOnClose = true;

        root.AddChild(control);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                Enum.GetNames<UITestControl.Tab>(),
                Loc.GetString("cmd-uitest2-arg-tab"));
        }

        return CompletionResult.Empty;
    }
}
