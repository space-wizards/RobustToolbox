using Lidgren.Network;
using SS14.Client.Interfaces.Console;
using SS14.Shared;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Maths;
using SS14.Shared.Network;
using SS14.Shared.Network.Messages;
using SS14.Shared.Reflection;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;

namespace SS14.Client.UserInterface
{
    // Disable reflection so that we won't be looked at for scene translation.
    [Reflect(false)]
    public class DebugConsole : Control, IDebugConsole
    {
        private LineEdit CommandBar;
        private Control LogContainer;
        private VScrollBar ScrollBar;

        readonly Dictionary<string, IConsoleCommand> _commands = new Dictionary<string, IConsoleCommand>();
        public IReadOnlyDictionary<string, IConsoleCommand> Commands => _commands;
        private bool sentCommandRequestToServer;

        protected override Godot.Control SpawnSceneControl()
        {
            var res = (Godot.PackedScene)Godot.ResourceLoader.Load("res://Scenes/DebugConsole/DebugConsole.tscn");
            var node = (Godot.Control)res.Instance();
            node.Visible = false;
            return node;
        }

        protected override void Initialize()
        {
            CommandBar = GetChild<LineEdit>("CommandBar");
            LogContainer = GetChild("ScrollContents").GetChild("VBoxContainer");
            ScrollBar = GetChild("ScrollContents").GetChild<VScrollBar>("_v_scroll");

            CommandBar.OnTextEntered += CommandEntered;

            InitializeCommands();
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

            var netMgr = IoCManager.Resolve<IClientNetManager>();
            if (Visible)
            {
                netMgr.MessageArrived += NetMgr_MessageArrived;
                if (netMgr.IsConnected && !sentCommandRequestToServer)
                    SendServerCommandRequest();
            }
            else
            {
                netMgr.MessageArrived -= NetMgr_MessageArrived;
            }
        }

        private void CommandEntered(LineEdit.LineEditEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(args.Text))
            {
                return;
            }

            CommandBar.Clear();
            AddLine($"> {args.Text}", new Color(255, 250, 240));

            ProcessCommand(args.Text);
        }

        private void ProcessCommand(string text)
        {
            //Commands are processed locally and then sent to the server to be processed there again.
            var args = new List<string>();

            CommandParsing.ParseArguments(text, args);

            var commandname = args[0];

            var forward = true;
            if (_commands.ContainsKey(commandname))
            {
                var command = _commands[commandname];
                args.RemoveAt(0);
                try
                {
                    forward = command.Execute(this, args.ToArray());
                }
                catch (Exception e)
                {
                    AddLine($"There was an exception while executing the command:\n{e}", Color.Red);
                    forward = false;
                }
            }
            else if (!IoCManager.Resolve<IClientNetManager>().IsConnected)
            {
                AddLine($"Unknown command: '{commandname}'", Color.Red);
                return;
            }

            if (forward)
                SendServerConsoleCommand(text);
        }

        public void AddLine(string text, Color color)
        {
            var atBottom = ScrollBar.IsAtBottom;
            var newtext = new Label
            {
                Text = text,
                //AutoWrap = true,
            };
            newtext.AddColorOverride("font_color", color);
            LogContainer.AddChild(newtext);
            if (atBottom)
            {
                ScrollBar.Value = ScrollBar.MaxValue - ScrollBar.Page;
            }
        }

        public void Clear()
        {
            LogContainer.DisposeAllChildren();
        }

        private void InitializeCommands()
        {
            var manager = IoCManager.Resolve<IReflectionManager>();
            foreach (var t in manager.GetAllChildren<IConsoleCommand>())
            {
                var instance = Activator.CreateInstance(t, null) as IConsoleCommand;
                if (_commands.ContainsKey(instance.Command))
                {
                    throw new Exception($"Command already registered: {instance.Command}");
                }

                _commands[instance.Command] = instance;
            }
        }

        private void SendServerConsoleCommand(string text)
        {
            var netMgr = IoCManager.Resolve<IClientNetManager>();
            if (netMgr != null && netMgr.IsConnected)
            {
                var outMsg = netMgr.CreateMessage();
                outMsg.Write((byte)NetMessages.ConsoleCommand);
                outMsg.Write(text);
                netMgr.ClientSendMessage(outMsg, NetDeliveryMethod.ReliableUnordered);
            }
        }

        private void SendServerCommandRequest()
        {
            var netMgr = IoCManager.Resolve<IClientNetManager>();
            if (!netMgr.IsConnected)
                return;

            var msg = netMgr.CreateNetMessage<MsgConCmdReg>();
            // empty message to request commands
            netMgr.ClientSendMessage(msg, NetDeliveryMethod.ReliableUnordered);

            sentCommandRequestToServer = true;
        }

        private void NetMgr_MessageArrived(object sender, NetMessageArgs e)
        {
            //Make sure we reset the position - we might receive this message after the gamestates.
            if (e.RawMessage.Position > 0)
                e.RawMessage.Position = 0;

            if (e.RawMessage.MessageType != NetIncomingMessageType.Data)
                return;

            switch ((NetMessages)e.RawMessage.PeekByte())
            {
                case NetMessages.ConsoleCommandReply:
                    e.RawMessage.ReadByte();
                    AddLine($"< {e.RawMessage.ReadString()}", new Color(65, 105, 225));
                    break;

                case NetMessages.ConsoleCommandRegister:
                    e.RawMessage.ReadByte();
                    for (var amount = e.RawMessage.ReadUInt16(); amount > 0; amount--)
                    {
                        var commandName = e.RawMessage.ReadString();
                        // Do not do duplicate commands.
                        if (_commands.ContainsKey(commandName))
                        {
                            AddLine($"Server sent console command {commandName}, but we already have one with the same name. Ignoring.", Color.White);
                            continue;
                        }

                        var description = e.RawMessage.ReadString();
                        var help = e.RawMessage.ReadString();

                        var command = new ServerDummyCommand(commandName, help, description);
                        _commands[commandName] = command;
                    }
                    break;
            }

            //Again, make sure we reset the position - we might get it before the gamestate and then that would break.
            e.RawMessage.Position = 0;
        }
    }

    /// <summary>
    ///     These dummies are made purely so list and help can list server-side commands.
    /// </summary>
    [Reflect(false)]
    internal class ServerDummyCommand : IConsoleCommand
    {
        internal ServerDummyCommand(string command, string help, string description)
        {
            Command = command;
            Help = help;
            Description = description;
        }

        public string Command { get; }

        public string Help { get; }

        public string Description { get; }

        // Always forward to server.
        public bool Execute(IDebugConsole console, params string[] args)
        {
            return true;
        }
    }
}
