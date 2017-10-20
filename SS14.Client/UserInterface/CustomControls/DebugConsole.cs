using System;
using System.Collections.Generic;
using Lidgren.Network;
using OpenTK.Graphics;
using SS14.Client.Graphics.Input;
using SS14.Client.Interfaces.Console;
using SS14.Client.Interfaces.Resource;
using SS14.Client.UserInterface.Components;
using SS14.Client.UserInterface.Controls;
using SS14.Shared;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.IoC;
using SS14.Shared.Network;
using SS14.Shared.Reflection;
using SS14.Shared.Utility;
using Vector2i = SS14.Shared.Maths.Vector2i;

namespace SS14.Client.UserInterface.CustomControls
{
    public class DebugConsole : ScrollableContainer, IDebugConsole
    {
        private Textbox input;
        private int last_y = 0;
        private Dictionary<string, IConsoleCommand> commands = new Dictionary<string, IConsoleCommand>();
        private bool sentCommandRequestToServer = false;

        public IDictionary<string, IConsoleCommand> Commands => commands;

        public DebugConsole(string uniqueName, Vector2i size, IResourceCache resourceCache) : base(uniqueName, size, resourceCache)
        {
            input = new Textbox(size.X)
            {
                ClearFocusOnSubmit = false,
                BackgroundColor = new Color4(64, 64, 64, 100),
                ForegroundColor = new Color4(255, 250, 240, 255)
            };
            input.OnSubmit += input_OnSubmit;
            this.BackgroundColor = new Color4(64, 64, 64, 200);
            this.DrawBackground = true;
            this.DrawBorder = true;

            InitializeCommands();
        }

        private void input_OnSubmit(string text, Textbox sender)
        {
            AddLine("> " + text, new Color4(255, 250, 240, 255));
            ProcessCommand(text);
        }

        public void AddLine(string text, Color4 color)
        {
            bool atBottom = ScrollbarV.Value >= ScrollbarV.max;
            Label newLabel = new Label(text, "CALIBRI")
            {
                Position = new Vector2i(5, last_y),
                ForegroundColor = color
            };

            newLabel.Update(0);
            last_y = newLabel.ClientArea.Bottom;
            Components.Add(newLabel);
            if (atBottom)
            {
                Update(0);
                ScrollbarV.Value = ScrollbarV.max;
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            if (input != null)
            {
                input.Position = new Vector2i(ClientArea.Left, ClientArea.Bottom);
                input.Update(frameTime);
            }
        }

        public override void ToggleVisible()
        {
            var netMgr = IoCManager.Resolve<IClientNetManager>();
            base.ToggleVisible();
            if (IsVisible())
            {
                // Focus doesn't matter because UserInterfaceManager is hardcoded to go to console when it's visible.
                // Though TextBox does like focus for the caret and passing KeyDown.
                input.Focus = true;
                netMgr.MessageArrived += NetMgr_MessageArrived;
                if (netMgr.IsConnected && !sentCommandRequestToServer)
                {
                    SendServerCommandRequest();
                }
            }
            else
            {
                input.Focus = false;
                netMgr.MessageArrived -= NetMgr_MessageArrived;
            }
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
                    AddLine("< " + e.RawMessage.ReadString(), new Color4(65, 105, 225, 255));
                    break;

                case NetMessages.ConsoleCommandRegister:
                    e.RawMessage.ReadByte();
                    for (ushort amount = e.RawMessage.ReadUInt16(); amount > 0; amount--)
                    {
                        string commandName = e.RawMessage.ReadString();
                        // Do not do duplicate commands.
                        if (commands.ContainsKey(commandName))
                        {
                            AddLine("Server sent console command {0}, but we already have one with the same name. Ignoring." + commandName, Color4.White);
                            continue;
                        }

                        string description = e.RawMessage.ReadString();
                        string help = e.RawMessage.ReadString();

                        var command = new ServerDummyCommand(commandName, help, description);
                        commands[commandName] = command;
                    }
                    break;
            }

            //Again, make sure we reset the position - we might get it before the gamestate and then that would break.
            e.RawMessage.Position = 0;
        }

        public override void Draw()
        {
            base.Draw();
            if (input != null) input.Draw();
        }

        public override void Dispose()
        {
            base.Dispose();
            input.Dispose();
        }

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (!base.MouseDown(e))
                if (input.MouseDown(e))
                {
                    // Focus doesn't matter because UserInterfaceManager is hardcoded to go to console when it's visible.
                    return true;
                }
            return false;
        }

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            if (!base.MouseUp(e))
                return input.MouseUp(e);
            else return false;
        }

        public override void MouseMove(MouseMoveEventArgs e)
        {
            base.MouseMove(e);
            input.MouseMove(e);
        }

        public override bool KeyDown(KeyEventArgs e)
        {
            if (!base.KeyDown(e))
                return input.KeyDown(e);
            else return false;
        }

        public override bool TextEntered(TextEventArgs e)
        {
            if (!base.TextEntered(e))
                return input.TextEntered(e);
            else return false;
        }

        /// <summary>
        /// Processes commands (chat messages starting with /)
        /// </summary>
        /// <param name="text">input text</param>
        private void ProcessCommand(string text)
        {
            //Commands are processed locally and then sent to the server to be processed there again.
            var args = new List<string>();

            CommandParsing.ParseArguments(text, args);

            string commandname = args[0];

            bool forward = true;
            if (commands.ContainsKey(commandname))
            {
                IConsoleCommand command = commands[commandname];
                args.RemoveAt(0);
                forward = command.Execute(this, args.ToArray());
            }
            else if (!IoCManager.Resolve<IClientNetManager>().IsConnected)
            {
                AddLine("Unknown command: " + commandname, Color4.Red);
                return;
            }

            if (forward)
            {
                SendServerConsoleCommand(text);
            }
        }

        private void InitializeCommands()
        {
            var manager = IoCManager.Resolve<IReflectionManager>();
            foreach (Type t in manager.GetAllChildren<IConsoleCommand>())
            {
                var instance = Activator.CreateInstance(t, null) as IConsoleCommand;
                if (commands.ContainsKey(instance.Command))
                    throw new Exception(string.Format("Command already registered: {0}", instance.Command));

                commands[instance.Command] = instance;
            }
        }

        private void SendServerConsoleCommand(string text)
        {
            var netMgr = IoCManager.Resolve<IClientNetManager>();
            if (netMgr != null && netMgr.IsConnected)
            {
                NetOutgoingMessage outMsg = netMgr.CreateMessage();
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

            NetOutgoingMessage outMsg = netMgr.CreateMessage();
            outMsg.Write((byte)NetMessages.ConsoleCommandRegister);
            netMgr.ClientSendMessage(outMsg, NetDeliveryMethod.ReliableUnordered);
            sentCommandRequestToServer = true;
        }

        public void Clear()
        {
            Components.Clear();
            last_y = 0;
            ScrollbarV.Value = 0;
        }
    }

    /// <summary>
    /// These dummies are made purely so list and help can list server-side commands.
    /// </summary>
    [Reflect(false)]
    class ServerDummyCommand : IConsoleCommand
    {
        readonly string command;
        readonly string help;
        readonly string description;

        public string Command => command;
        public string Help => help;
        public string Description => description;

        internal ServerDummyCommand(string command, string help, string description)
        {
            this.command = command;
            this.help = help;
            this.description = description;
        }

        // Always forward to server.
        public bool Execute(IDebugConsole console, params string[] args)
        {
            return true;
        }
    }
}
