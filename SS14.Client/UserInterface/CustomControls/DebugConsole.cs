using System;
using System.Collections.Generic;
using Lidgren.Network;
using OpenTK.Graphics;
using SS14.Client.Graphics.Input;
using SS14.Client.Interfaces.Console;
using SS14.Client.UserInterface.Controls;
using SS14.Shared;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.IoC;
using SS14.Shared.Network;
using SS14.Shared.Network.Messages;
using SS14.Shared.Reflection;
using SS14.Shared.Utility;
using Vector2i = SS14.Shared.Maths.Vector2i;

namespace SS14.Client.UserInterface.CustomControls
{
    public class DebugConsole : ScrollableContainer, IDebugConsole
    {
        private readonly Textbox _txtInput;
        private readonly ListPanel _historyList;
        private readonly Dictionary<string, IConsoleCommand> _commands = new Dictionary<string, IConsoleCommand>();

        private int last_y;
        private bool sentCommandRequestToServer;

        public override bool Visible
        {
            get => base.Visible;
            set
            {
                base.Visible = value;

                var netMgr = IoCManager.Resolve<IClientNetManager>();
                if (value)
                {
                    // Focus doesn't matter because UserInterfaceManager is hardcoded to go to console when it's visible.
                    // Though TextBox does like focus for the caret and passing KeyDown.
                    _txtInput.Focus = true;
                    netMgr.MessageArrived += NetMgr_MessageArrived;
                    if (netMgr.IsConnected && !sentCommandRequestToServer)
                        SendServerCommandRequest();
                }
                else
                {
                    _txtInput.Focus = false;
                    netMgr.MessageArrived -= NetMgr_MessageArrived;
                }
            }
        }

        public DebugConsole(string uniqueName, Vector2i size)
            : base(size)
        {
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

            InitializeCommands();
        }

        public IReadOnlyDictionary<string, IConsoleCommand> Commands => _commands;

        public void AddLine(string text, Color4 color)
        {
            var atBottom = ScrollbarV.Value >= ScrollbarV.Max;
            var newLabel = new Label(text, "CALIBRI")
            {
                Position = new Vector2i(5, last_y),
                ForegroundColor = color
            };
            
            last_y = newLabel.ClientArea.Bottom;
            _historyList.AddControl(newLabel);
            _historyList.DoLayout();


            if (atBottom)
            {
                Update(0);
                ScrollbarV.Value = ScrollbarV.Max;
            }
        }

        public void Clear()
        {
            _historyList.DisposeAllChildren();
            _historyList.DoLayout();
            last_y = 0;
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
            AddLine("> " + text, new Color4(255, 250, 240, 255));

            if(!string.IsNullOrWhiteSpace(text))
                ProcessCommand(text);
        }

        private void NetMgr_MessageArrived(object sender, NetMessageArgs e)
        {
            //Make sure we reset the position - we might receive this message after the gamestates.
            if (e.RawMessage.Position > 0)
                e.RawMessage.Position = 0;

            if (e.RawMessage.MessageType != NetIncomingMessageType.Data)
                return;

            switch ((NetMessages) e.RawMessage.PeekByte())
            {
                case NetMessages.ConsoleCommandReply:
                    e.RawMessage.ReadByte();
                    AddLine("< " + e.RawMessage.ReadString(), new Color4(65, 105, 225, 255));
                    break;

                case NetMessages.ConsoleCommandRegister:
                    e.RawMessage.ReadByte();
                    for (var amount = e.RawMessage.ReadUInt16(); amount > 0; amount--)
                    {
                        var commandName = e.RawMessage.ReadString();
                        // Do not do duplicate commands.
                        if (_commands.ContainsKey(commandName))
                        {
                            AddLine("Server sent console command {0}, but we already have one with the same name. Ignoring." + commandName, Color4.White);
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

        /// <summary>
        ///     Processes commands (chat messages starting with /)
        /// </summary>
        /// <param name="text">input text</param>
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
                forward = command.Execute(this, args.ToArray());
            }
            else if (!IoCManager.Resolve<IClientNetManager>().IsConnected)
            {
                AddLine("Unknown command: " + commandname, Color4.Red);
                return;
            }

            if (forward)
                SendServerConsoleCommand(text);
        }

        private void InitializeCommands()
        {
            var manager = IoCManager.Resolve<IReflectionManager>();
            foreach (var t in manager.GetAllChildren<IConsoleCommand>())
            {
                var instance = Activator.CreateInstance(t, null) as IConsoleCommand;
                if (_commands.ContainsKey(instance.Command))
                    throw new Exception(string.Format("Command already registered: {0}", instance.Command));

                _commands[instance.Command] = instance;
            }
        }

        private void SendServerConsoleCommand(string text)
        {
            var netMgr = IoCManager.Resolve<IClientNetManager>();
            if (netMgr != null && netMgr.IsConnected)
            {
                var outMsg = netMgr.CreateMessage();
                outMsg.Write((byte) NetMessages.ConsoleCommand);
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
