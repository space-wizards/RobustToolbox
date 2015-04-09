using Lidgren.Network;
using SFML.Window;
using SS14.Client.Interfaces.GOC;
using SS14.Client.Interfaces.Network;
using SS14.Client.Interfaces.Player;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.UserInterface;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Drawing;


namespace SS14.Client.Services.UserInterface.Components
{
    public class DebugConsole : ScrollableContainer
    {
        private Textbox input;
        private List<Label> lines = new List<Label>();
        private int last_y = 0;

        public DebugConsole(string uniqueName, Size size, IResourceManager resourceManager) : base(uniqueName, size, resourceManager)
        {
            input = new Textbox(size.Width, resourceManager);
            input.ClearFocusOnSubmit = false;
            input.drawColor = Color.FromArgb(100, Color.Gray);
            input.textColor = Color.FloralWhite;
            input.OnSubmit += new Textbox.TextSubmitHandler(input_OnSubmit);
            this.BackgroundColor = Color.FromArgb(100, Color.Gray);
            this.DrawBackground = true;
            this.DrawBorder = true;
            Update(0);
        }

        void input_OnSubmit(string text, Textbox sender)
        {
            AddLine(text, Color.FloralWhite);
            ProcessCommand(text);
        }

        public void AddLine(string text, Color color)
        {
            Label newLabel = new Label(text, "MICROGBE", this._resourceManager);
            newLabel.Position = new Point(5, last_y);
            newLabel.TextColor = color;
            newLabel.Update(0);
            last_y = newLabel.ClientArea.Bottom;
            components.Add(newLabel);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            if (input != null)
            {
                input.Position = new Point(ClientArea.Left, ClientArea.Bottom);
                input.Update(frameTime);
            }
        }

        public override void ToggleVisible()
        {
            var netMgr = IoCManager.Resolve<INetworkManager>();
            base.ToggleVisible();
            if (IsVisible())
            {
                IoCManager.Resolve<IUserInterfaceManager>().SetFocus(input);
                netMgr.MessageArrived += new EventHandler<IncomingNetworkMessageArgs>(netMgr_MessageArrived);
            }
            else
            {
                netMgr.MessageArrived -= new EventHandler<IncomingNetworkMessageArgs>(netMgr_MessageArrived);
            }
        }

        void netMgr_MessageArrived(object sender, IncomingNetworkMessageArgs e)
        {
            //Make sure we reset the position - we might recieve this message after the gamestates.
            if (e.Message.Position > 0) e.Message.Position = 0;
            if (e.Message.MessageType == NetIncomingMessageType.Data && (NetMessage)e.Message.PeekByte() == NetMessage.ConsoleCommandReply)
            {
                e.Message.ReadByte();
                AddLine("Server: " + e.Message.ReadString(), Color.RoyalBlue);
            }
            //Again, make sure we reset the position - we might get it before the gamestate and then that would break.
            e.Message.Position = 0;
        }

        public override void Render()
        {
            base.Render();
            if (input != null) input.Render();
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
                    IoCManager.Resolve<IUserInterfaceManager>().SetFocus(input);
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

		public override bool MouseWheelMove(MouseWheelEventArgs e)
        {
            if (!base.MouseWheelMove(e))
                return input.MouseWheelMove(e);
            else return false;
        }

        public override bool KeyDown(KeyEventArgs e)
        {
            if (!base.KeyDown(e))
                return input.KeyDown(e);
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

            string command = args[0];

            //Entity player;
            //var entMgr = IoCManager.Resolve<IEntityManager>();
            //var plrMgr = IoCManager.Resolve<IPlayerManager>();
            //player = plrMgr.ControlledEntity;
            //IoCManager.Resolve<INetworkManager>().

            switch (command)
            {
                case "cls":
                    lines.Clear();
                    components.Clear();
                    last_y = 0;
                    //this.scrollbarH.Value = 0;
                    this.scrollbarV.Value = 0;
                    break;
                case "quit":
                    Environment.Exit(0);
                    break;
                case "addparticles": //This is only clientside.
                    if (args.Count >= 3)
                    {
                        Entity target = null;
                        if (args[1].ToLowerInvariant() == "player")
                        {
                            var plrMgr = IoCManager.Resolve<IPlayerManager>();
                            if (plrMgr != null)
                                if (plrMgr.ControlledEntity != null) target = plrMgr.ControlledEntity;
                        }
                        else
                        {
                            var entMgr = IoCManager.Resolve<IEntityManagerContainer>();
                            if (entMgr != null)
                            {
                                int entUid = int.Parse(args[1]);
                                target = entMgr.EntityManager.GetEntity(entUid);
                            }
                        }

                        if (target != null)
                        {
                            if (!target.HasComponent(ComponentFamily.Particles))
                            {
                                var entMgr = IoCManager.Resolve<IEntityManagerContainer>();
                                var compo = (IParticleSystemComponent)entMgr.EntityManager.ComponentFactory.GetComponent("ParticleSystemComponent");
                                target.AddComponent(ComponentFamily.Particles, compo);
                            }
                            else
                            {
                                var entMgr = IoCManager.Resolve<IEntityManagerContainer>();
                                var compo = (IParticleSystemComponent)entMgr.EntityManager.ComponentFactory.GetComponent("ParticleSystemComponent");
                                target.AddComponent(ComponentFamily.Particles, compo);                                
                            }
                        }
                    }
                    SendServerConsoleCommand(text); //Forward to server.
                    break;
                case "removeparticles":
                    if (args.Count >= 3)
                    {
                        Entity target = null;
                        if (args[1].ToLowerInvariant() == "player")
                        {
                            var plrMgr = IoCManager.Resolve<IPlayerManager>();
                            if (plrMgr != null)
                                if (plrMgr.ControlledEntity != null) target = plrMgr.ControlledEntity;
                        }
                        else
                        {
                            var entMgr = IoCManager.Resolve<IEntityManagerContainer>();
                            if (entMgr != null)
                            {
                                int entUid = int.Parse(args[1]);
                                target = entMgr.EntityManager.GetEntity(entUid);
                            }
                        }

                        if (target != null)
                        {
                            if (target.HasComponent(ComponentFamily.Particles))
                            {
                                IParticleSystemComponent compo = (IParticleSystemComponent)target.GetComponent(ComponentFamily.Particles);
                                compo.RemoveParticleSystem(args[2]);
                            }
                        }
                    }
                    SendServerConsoleCommand(text); //Forward to server.
                    break;
                default:
                    SendServerConsoleCommand(text); //Forward to server.
                    break;
            }
        }

        private void SendServerConsoleCommand(string text)
        {
            var netMgr = IoCManager.Resolve<INetworkManager>();
            if (netMgr != null && netMgr.IsConnected)
            {
                NetOutgoingMessage outMsg = netMgr.CreateMessage();
                outMsg.Write((byte) NetMessage.ConsoleCommand);
                outMsg.Write(text);
                netMgr.SendMessage(outMsg, NetDeliveryMethod.ReliableUnordered);
            }
        }
    }
}