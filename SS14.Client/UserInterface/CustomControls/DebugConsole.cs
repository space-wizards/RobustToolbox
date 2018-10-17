using SS14.Client.Console;
using SS14.Client.Interfaces.Console;
using SS14.Client.UserInterface.Controls;
using SS14.Client.Utility;
using SS14.Shared.Console;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Reflection;
using System;
using System.Collections.Generic;
using SS14.Shared.Utility;

namespace SS14.Client.UserInterface.CustomControls
{
    public class DebugConsole : Control, IDebugConsole
    {
        [Dependency]
        readonly IClientConsole console;

        private bool firstLine = true;
        private LineEdit CommandBar;
        private RichTextLabel Contents;

        public IReadOnlyDictionary<string, IConsoleCommand> Commands => console.Commands;

        protected override ResourcePath ScenePath => new ResourcePath("/Scenes/DebugConsole/DebugConsole.tscn");

        protected override void Initialize()
        {
            IoCManager.InjectDependencies(this);

            Visible = false;

            CommandBar = GetChild<LineEdit>("CommandBar");
            Contents = GetChild<RichTextLabel>("Contents");
            Contents.ScrollFollowing = true;

            CommandBar.OnTextEntered += CommandEntered;

            console.AddString += (_, args) => AddLine(args.Text, args.Channel, args.Color);
            console.AddFormatted += (_, args) => AddFormattedLine(args.Message);
            console.ClearText += (_, args) => Clear();
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
                CommandBar.Clear();
            }
        }

        public void AddLine(string text, ChatChannel channel, Color color)
        {
            if (!firstLine)
            {
                Contents.NewLine();
            }
            else
            {
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

        public void AddFormattedLine(FormattedMessage message)
        {
            if (!firstLine)
            {
                Contents.NewLine();
            }
            else
            {
                firstLine = false;
            }

            var pushCount = 0;
            foreach (var tag in message.Tags)
            {
                switch (tag)
                {
                    case FormattedMessage.TagText text:
                        Contents.AddText(text.Text);
                        break;
                    case FormattedMessage.TagColor color:
                        Contents.PushColor(color.Color);
                        pushCount++;
                        break;
                    case FormattedMessage.TagPop pop:
                        if (pushCount <= 0)
                        {
                            throw new InvalidOperationException();
                        }
                        Contents.Pop();
                        pushCount--;
                        break;
                }
            }

            for (; pushCount > 0; pushCount--)
            {
                Contents.Pop();
            }
        }

        public void Clear()
        {
            Contents.Clear();
            firstLine = true;
        }
    }
}
