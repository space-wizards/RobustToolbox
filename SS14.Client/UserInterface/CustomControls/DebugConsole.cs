<<<<<<< HEAD
﻿using Lidgren.Network;
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
=======
﻿using System.Collections.Generic;
using OpenTK.Graphics;
using SS14.Client.Console;
using SS14.Client.Graphics.Input;
using SS14.Client.Interfaces.Console;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.Console;
using SS14.Shared.IoC;
using Vector2i = SS14.Shared.Maths.Vector2i;
>>>>>>> upstream/master

namespace SS14.Client.UserInterface
{
    // Disable reflection so that we won't be looked at for scene translation.
    [Reflect(false)]
    public class DebugConsole : Control, IDebugConsole
    {
<<<<<<< HEAD
        private LineEdit CommandBar;
        private RichTextLabel Contents;

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
            Contents = GetChild<RichTextLabel>("Contents");
            Contents.ScrollFollowing = true;

            CommandBar.OnTextEntered += CommandEntered;

            InitializeCommands();
        }

        public void Toggle()
=======
        private readonly IClientConsole _console;
        private readonly Textbox _txtInput;
        private readonly ListPanel _historyList;

        private int _lastY;
        
        public DebugConsole(Vector2i size)
            : base(size)
        {
            _console = IoCManager.Resolve<IClientConsole>();
            _txtInput = new Textbox(size.X)
            {
                ClearFocusOnSubmit = false,
                BackgroundColor = new Color4(64, 64, 64, 100),
                ForegroundColor = new Color4(255, 250, 240, 255)
            };
            _txtInput.OnSubmit += TxtInputOnSubmit;

            _historyList = new ListPanel();
            Container.AddControl(_historyList);

            BackgroundColor = new Color4(64, 64, 64, 200);
            DrawBackground = true;
            DrawBorder = true;
            
            _console.AddString += (sender, args) => AddLine(args.Text, args.Channel, args.Color);
            _console.ClearText += (sender, args) => Clear();
        }

        public IReadOnlyDictionary<string, IConsoleCommand> Commands => _console.Commands;

        public void AddLine(string text, ChatChannel channel, Color4 color)
>>>>>>> upstream/master
        {
            var focus = CommandBar.HasFocus();
            Visible = !Visible;
            if (Visible)
            {
<<<<<<< HEAD
                CommandBar.GrabFocus();
            }
            else if (focus)
=======
                Position = new Vector2i(5, _lastY),
                ForegroundColor = color
            };
            
            _lastY = newLabel.ClientArea.Bottom;
            _historyList.AddControl(newLabel);
            _historyList.DoLayout();


            if (atBottom)
>>>>>>> upstream/master
            {
                // We manually need to call this.
                // See https://github.com/godotengine/godot/pull/15074
                UserInterfaceManager.FocusExited(CommandBar);
            }
<<<<<<< HEAD

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
            Contents.NewLine();
            Contents.PushColor(color);
            Contents.AddText(text);
            Contents.Pop(); // Pop the color off.
        }

        public void Clear()
        {
            Contents.Clear();
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
=======
        }

        public void Clear()
        {
            _historyList.DisposeAllChildren();
            _historyList.DoLayout();
            _lastY = 0;
            ScrollbarV.Value = 0;
        }

        public override void DoLayout()
        {
            base.DoLayout();

            _txtInput.LocalPosition = Position + new Vector2i(ClientArea.Left, ClientArea.Bottom);
            _txtInput.DoLayout();
        }
        
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            _txtInput.Update(frameTime);
        }

        public override void Draw()
        {
            base.Draw();
            _txtInput.Draw();
        }

        public override void Dispose()
        {
            _txtInput.Dispose();
            _console.Dispose();
            base.Dispose();
        }

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (!base.MouseDown(e))
                if (_txtInput.MouseDown(e))
                    return true;
            return false;
        }

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            if (!base.MouseUp(e))
                return _txtInput.MouseUp(e);
            return false;
        }

        public override void MouseMove(MouseMoveEventArgs e)
        {
            base.MouseMove(e);
            _txtInput.MouseMove(e);
        }

        public override bool KeyDown(KeyEventArgs e)
        {
            if (!base.KeyDown(e))
                return _txtInput.KeyDown(e);
            return false;
        }

        public override bool TextEntered(TextEventArgs e)
        {
            if (!base.TextEntered(e))
                return _txtInput.TextEntered(e);
            return false;
        }

        private void TxtInputOnSubmit(Textbox sender, string text)
        {
            // debugConsole input is not prefixed with slash
            if(!string.IsNullOrWhiteSpace(text))
                _console.ProcessCommand(text);
>>>>>>> upstream/master
        }
    }
}
