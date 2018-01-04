using SS14.Client.Console;
using SS14.Client.Interfaces.Console;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.Console;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Reflection;
using System.Collections.Generic;

namespace SS14.Client.UserInterface.CustomControls
{
    // Disable reflection so that we won't be looked at for scene translation.
    [Reflect(false)]
    public class DebugConsole : Control, IDebugConsole
    {
        [Dependency]
        readonly IClientConsole console;

        private bool firstLine = true;
        private LineEdit CommandBar;
        private RichTextLabel Contents;

        public IReadOnlyDictionary<string, IConsoleCommand> Commands => console.Commands;

        protected override Godot.Control SpawnSceneControl()
        {
            var res = (Godot.PackedScene)Godot.ResourceLoader.Load("res://Scenes/DebugConsole/DebugConsole.tscn");
            var node = (Godot.Control)res.Instance();
            node.Visible = false;
            return node;
        }

        protected override void Initialize()
        {
            IoCManager.InjectDependencies(this);

            CommandBar = GetChild<LineEdit>("CommandBar");
            Contents = GetChild<RichTextLabel>("Contents");
            Contents.ScrollFollowing = true;

            CommandBar.OnTextEntered += CommandEntered;

            console.AddString += (sender, args) => AddLine(args.Text, args.Channel, args.Color);
            console.ClearText += (sender, args) => Clear();
        }

        public void Toggle()
        {
            var focus = CommandBar.HasFocus();
            Visible = !Visible;
            if (Visible)
            {
                CommandBar.GrabFocus();
            }
            else if (focus)
            {
                // We manually need to call this.
                // See https://github.com/godotengine/godot/pull/15074
                UserInterfaceManager.FocusExited(CommandBar);
            }
        }

        private void CommandEntered(LineEdit.LineEditEventArgs args)
        {
            if (!string.IsNullOrWhiteSpace(args.Text))
            {
                console.ProcessCommand(args.Text);
            }
        }

        public void AddLine(string text, ChatChannel channel, Color color)
        {
            if (firstLine)
            {
                Contents.NewLine();
                firstLine = false;
            }
            Contents.PushColor(color);
            Contents.AddText(text);
            Contents.Pop(); // Pop the color off.
        }

        public void AddLine(string text, Color color)
        {
            AddLine(text, ChatChannel.Default, color);
        }

        public void AddLine(string text)
        {
            AddLine(text, ChatChannel.Default, Color.White);
        }

        public void Clear()
        {
            Contents.Clear();
            firstLine = true;
        }
    }
}
