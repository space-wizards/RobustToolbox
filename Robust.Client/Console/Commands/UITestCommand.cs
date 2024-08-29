using System;
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

internal sealed partial class UITestControl : Control
{
    private const string Lipsum = @"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Integer sed interdum diam. Duis erat risus, tincidunt at pulvinar non, accumsan non dui. Morbi feugiat nisi in odio consectetur, ac suscipit nulla mollis. Nulla consequat neque sit amet neque venenatis feugiat. Proin placerat eget mauris sit amet tincidunt. Sed pulvinar purus sed ex varius, et lobortis risus efficitur. Integer blandit eu neque quis elementum. Vivamus lacinia sem non lacinia eleifend. Integer sit amet est ac risus tempus iaculis sed quis leo. Proin eu dui tincidunt orci ornare elementum. Curabitur molestie enim scelerisque, porttitor ipsum vitae, posuere libero. Donec finibus placerat accumsan. Nam et arcu lacus.

Proin sed dui gravida nibh faucibus sodales ut sit amet dolor. Pellentesque ornare neque ac ante sagittis posuere. Maecenas ullamcorper pellentesque aliquet. Vestibulum ipsum ipsum, hendrerit eu venenatis eget, tempor aliquet ex. Etiam sed nunc eu orci condimentum consequat. Praesent commodo sem a lorem consequat, nec vestibulum elit dignissim. Sed fermentum maximus neque, non vestibulum felis. Quisque vulputate vehicula massa, sit amet accumsan purus condimentum nec. Ut tincidunt in purus sit amet lobortis. Nunc et eros vel elit sodales mollis. Aenean facilisis justo libero, at mollis arcu rutrum eget. Aenean rutrum, orci pretium faucibus auctor, tellus quam tincidunt diam, et feugiat turpis lectus nec sem.

Donec et ipsum urna. Vestibulum consequat risus vitae orci consectetur ornare id id ligula. Donec ac nunc venenatis, volutpat elit eget, eleifend ex. Fusce eget odio sed tortor luctus feugiat. Maecenas lobortis nulla sit amet nisl egestas vulputate. Aliquam a placerat nunc. Fusce porta ultricies tortor, vitae dictum elit aliquet ac. In massa sapien, lobortis laoreet odio dignissim, congue blandit nibh. Quisque et iaculis eros, sed pretium felis. Praesent venenatis porta odio sed vulputate. Vivamus lacus nulla, lacinia non commodo id, ultricies nec arcu. Donec scelerisque pretium mollis. Etiam eu facilisis leo.

Curabitur vulputate euismod massa, pulvinar tincidunt arcu vestibulum ut. Sed eu tempus velit, at porttitor justo. In eget turpis fermentum nibh euismod vestibulum. Proin vitae malesuada ipsum. Nunc at aliquet erat, sed maximus tortor. Cras tristique consequat elit, ut venenatis elit feugiat et. In malesuada, erat a tempus vehicula, nulla justo efficitur mauris, vitae ornare lectus massa eu sapien. Nam libero diam, gravida ac dapibus sed, hendrerit sed libero. Sed fringilla enim vel elit finibus congue. Fusce tristique, neque sit amet blandit posuere, ex urna malesuada ligula, ut sodales dolor est vitae lectus. Sed pharetra tincidunt pulvinar. Fusce sit amet finibus nulla, vel maximus tellus. Etiam in nisl ex. Fusce tempus augue lectus, eu sagittis arcu tempor id. Sed feugiat venenatis semper. Cras eget mollis nisi.

Suspendisse hendrerit blandit urna ut laoreet. Suspendisse ac elit at erat malesuada commodo id vel dolor. Etiam sem magna, placerat lobortis mattis a, tincidunt at nisi. Ut gravida arcu purus, eu feugiat turpis accumsan non. Sed sit amet varius enim, sed ornare ante. Integer porta felis felis. Vestibulum euismod velit sit amet eleifend posuere. Cras laoreet fermentum condimentum. Suspendisse potenti. Donec iaculis sodales vestibulum. Etiam quis dictum nisl. Fusce dui ex, viverra nec lacus sed, tincidunt accumsan odio. Nulla sit amet ipsum eros. Curabitur et lectus ut nisi lobortis sollicitudin a eu turpis. Etiam molestie purus vitae porttitor auctor.
";


    private readonly TabContainer _tabContainer;
    private readonly TabSpriteView _sprite;

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
                    MinSize = new(50, 50),
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

        _tabContainer.AddChild(TabTextEdit());
        _tabContainer.AddChild(TabRichText());

        _sprite = new TabSpriteView();
        _tabContainer.AddChild(_sprite);
    }

    public void OnClosed()
    {
        _sprite.OnClosed();
    }

    private Control TabTextEdit()
    {
        var textEdit = new TextEdit
        {
            Placeholder = new Rope.Leaf("You deleted the lipsum\nOwO")
        };
        TabContainer.SetTabTitle(textEdit, "TextEdit");

        var lipsumRope = new Rope.Branch(Rope.Leaf.Empty, null);

        var startIndex = 0;
        while (true)
        {
            var nextIndex = Lipsum.IndexOf(' ', startIndex);
            var str = nextIndex == -1 ? Lipsum[startIndex..] : Lipsum[startIndex..(nextIndex+1)];

            lipsumRope = new Rope.Branch(lipsumRope, new Rope.Leaf(str));
            if (lipsumRope.Depth > 250)
                lipsumRope = (Rope.Branch)Rope.Rebalance(lipsumRope);

            if (nextIndex == -1)
                break;

            startIndex = nextIndex + 1;
        }

        var rope = new Rope.Branch(lipsumRope, null);

        for (var i = 0; i < 10; i++)
        {
            rope = new Rope.Branch(rope, lipsumRope);
        }

        rope = (Rope.Branch) Rope.Rebalance(rope);

        textEdit.TextRope = rope;

        return textEdit;
    }

    private Control TabRichText()
    {
        var label = new RichTextLabel();
        label.SetMessage(FormattedMessage.FromMarkupOrThrow(Lipsum));

        TabContainer.SetTabTitle(label, "RichText");
        return label;
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
        TextEdit = 6,
        RichText = 7,
        SpriteView = 8,
    }
}

internal sealed class UITestCommand : LocalizedCommands
{
    public override string Command => "uitest";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var window = new DefaultWindow { MinSize = new(800, 600) };
        var control = new UITestControl();
        window.OnClose += control.OnClosed;
        window.Contents.AddChild(control);

        window.OpenCentered();
    }
}

internal sealed class UITest2Command : LocalizedCommands
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IUserInterfaceManager _uiMgr = default!;

    public override string Command => "uitest2";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
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
        window.RequestClosed += _ => control.OnClosed();
        root.AddChild(control);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
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
