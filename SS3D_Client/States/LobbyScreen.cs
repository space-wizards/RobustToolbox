using System;

using Lidgren.Network;

using SS3D.Modules;
using SS3D.Modules.Map;
using SS3D.Modules.Network;
using SS3D.Modules.UI;

using System.Collections.Generic;
using System.Reflection;

using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using SS3D.Modules.UI;
using SS3D.Modules.UI.Components;

namespace SS3D.States
{
    public class LobbyScreen : State
    {
        private StateManager mStateMgr;

        private int serverMaxPlayers;
        private string serverName;
        private int serverPort;
        private string welcomeString;
        private string serverMapName;
        private GameType gameType;


        private PlayerController playerController;
        private Chatbox lobbyChat;
        private Button joinButt;

        TextSprite lobbyText;

        public LobbyScreen()
        {
        }

        public override bool Startup(Program _prg)
        {
            prg = _prg;
            mStateMgr = prg.mStateMgr;
            PlayerController.Initialize(this);
            playerController = PlayerController.Singleton;

            prg.mNetworkMgr.MessageArrived += new NetworkMsgHandler(mNetworkMgr_MessageArrived);

            lobbyChat = new Modules.UI.Chatbox("lobbyChat");
            lobbyChat.TextSubmitted += new Modules.UI.Chatbox.TextSubmitHandler(lobbyChat_TextSubmitted);

            lobbyText = new TextSprite("lobbyText", "", ResMgr.Singleton.GetFont("CALIBRI"));
            lobbyText.Color = System.Drawing.Color.Black;
            lobbyText.ShadowColor = System.Drawing.Color.DimGray;
            lobbyText.Shadowed = true;
            lobbyText.ShadowOffset = new Vector2D(1, 1);

            NetworkManager netMgr = prg.mNetworkMgr;
            NetOutgoingMessage message = netMgr.netClient.CreateMessage();
            message.Write((byte)NetMessage.WelcomeMessage); //Request Welcome msg.
            netMgr.netClient.SendMessage(message, NetDeliveryMethod.ReliableOrdered);
            prg.mNetworkMgr.SendClientName(ConfigManager.Singleton.Configuration.PlayerName);

            joinButt = new Button("Join Game");
            joinButt.Clicked += new Button.ButtonPressHandler(joinButt_Clicked);

            Gorgon.Screen.Clear();

            //BYPASS LOBBY
            //playerController.SendVerb("joingame", 0);
            return true;
        }

        void joinButt_Clicked(Button sender)
        {
            playerController.SendVerb("joingame", 0);
        }

        void lobbyChat_TextSubmitted(Modules.UI.Chatbox Chatbox, string Text)
        {
            SendLobbyChat(Text);
        }

        public override void GorgonRender(FrameEventArgs e)
        {
            Gorgon.Screen.Clear();
            Gorgon.Screen.FilledRectangle(5, 30, 600, 200, System.Drawing.Color.SlateGray);
            Gorgon.Screen.FilledRectangle(625, 30, Gorgon.Screen.Width - 625 - 5, Gorgon.Screen.Height - 30 - 6, System.Drawing.Color.SlateGray);
            lobbyText.Position = new Vector2D(10, 35);
            lobbyText.Text = "Server: " + serverName;
            lobbyText.Draw();
            lobbyText.Position = new Vector2D(10, 55);
            lobbyText.Text = "Server-Port: "+ serverPort.ToString();
            lobbyText.Draw();
            lobbyText.Position = new Vector2D(10, 75);
            lobbyText.Text = "Max Players: " + serverMaxPlayers.ToString();
            lobbyText.Draw();
            lobbyText.Position = new Vector2D(10, 95);
            lobbyText.Text = "Gamemode: " + gameType.ToString();
            lobbyText.Draw();
            lobbyText.Position = new Vector2D(10, 135);
            lobbyText.Text = "MOTD: \n" + welcomeString;
            lobbyText.Draw();
            joinButt.Position = new System.Drawing.Point(Gorgon.Screen.Width - joinButt.Size.Width - 10, Gorgon.Screen.Height - joinButt.Size.Height - 10);
            joinButt.Render();
            return;
        }

        public override void FormResize()
        {
            throw new NotImplementedException();
        }

        void mNetworkMgr_MessageArrived(NetworkManager netMgr, NetIncomingMessage msg)
        {
            switch (msg.MessageType)
            {
                case NetIncomingMessageType.Data:
                    NetMessage messageType = (NetMessage)msg.ReadByte();
                    switch (messageType)
                    {
                        case NetMessage.LobbyChat:
                            string text = msg.ReadString();
                            AddChat(text);
                            break;
                        case NetMessage.PlayerCount:
                            int newCount = msg.ReadByte();
                            break;
                        case NetMessage.WelcomeMessage:
                            HandleWelcomeMessage(msg);
                            break;
                        case NetMessage.ChatMessage:
                            HandleChatMessage(msg);
                            break;
                        case NetMessage.JoinGame:
                            HandleJoinGame();
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }
        }

        private void HandleJoinGame()
        {
            mStateMgr.RequestStateChange(typeof(GameScreen));
        }

        private void AddChat(string text)
        {
            lobbyChat.AddLine(text, ChatChannel.Lobby);
        }

        public void SendLobbyChat(string text)
        {
            NetOutgoingMessage message = prg.mNetworkMgr.netClient.CreateMessage();
            message.Write((byte)NetMessage.ChatMessage);
            message.Write((byte)ChatChannel.Lobby);
            message.Write(text);

            prg.mNetworkMgr.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
        }

        public override void Shutdown()
        {
            lobbyChat.Dispose();
            lobbyChat = null;
            joinButt.Dispose();
            joinButt = null;
            prg.mNetworkMgr.MessageArrived -= new NetworkMsgHandler(mNetworkMgr_MessageArrived);
        }

        public override void Update(FrameEventArgs e)
        {
            joinButt.Update();
        }

        private void HandleWelcomeMessage(NetIncomingMessage msg)
        {
            serverName = msg.ReadString();
            serverPort = msg.ReadInt32();
            welcomeString = msg.ReadString();
            serverMaxPlayers = msg.ReadInt32();
            serverMapName = msg.ReadString();
            gameType = (GameType)msg.ReadByte();
        }

        private void HandleChatMessage(NetIncomingMessage msg)
        {
            ChatChannel channel = (ChatChannel)msg.ReadByte();
            if (channel != ChatChannel.Lobby) return; //NOPE
            string text = msg.ReadString();
            string message = "(" + channel.ToString() + "):" + text;
            ushort atomID = msg.ReadUInt16();
            lobbyChat.AddLine(message, ChatChannel.Lobby);
        }

        #region Input
 
        public override void KeyDown(KeyboardInputEventArgs e)
        { }
        public override void KeyUp(KeyboardInputEventArgs e)
        { }
        public override void MouseUp(MouseInputEventArgs e)
        { }
        public override void MouseDown(MouseInputEventArgs e)
        {
            joinButt.MouseDown(e);
        }
        public override void MouseMove(MouseInputEventArgs e)
        { }
        #endregion
    }
}
