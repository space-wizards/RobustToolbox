using System;

using Mogre;
using Lidgren.Network;
using Miyagi;
using Miyagi.UI;
using Miyagi.UI.Controls;
using Miyagi.Common;
using Miyagi.Common.Data;
using Miyagi.Common.Resources;
using Miyagi.Common.Events;
using Miyagi.TwoD;

using SS3D.Modules;
using SS3D.Modules.Map;
using SS3D.Modules.Network;
using SS3D.Modules.UI;

using System.Collections.Generic;
using System.Reflection;


namespace SS3D.States
{
    public class LobbyScreen : State
    {
        private StateManager mStateMgr;
        private int serverMaxPlayers;
        private Chatbox lobbyChat;
        private GUI guiBackground;
        private GUI guiLobbyMenu;
        private PlayerController playerController;

        public LobbyScreen()
        {
            mEngine = null;
        }

        public override bool Startup(StateManager _mgr)
        {
            mEngine = _mgr.Engine;
            mStateMgr = _mgr;
            playerController = new PlayerController(this);

            mEngine.mNetworkMgr.MessageArrived += new NetworkMsgHandler(mNetworkMgr_MessageArrived);

            CreateGUI();
 
            NetworkManager netMgr = mEngine.mNetworkMgr;
            NetOutgoingMessage message = netMgr.netClient.CreateMessage();
            message.Write((byte)NetMessage.WelcomeMessage); //Request Welcome msg.
            netMgr.netClient.SendMessage(message, NetDeliveryMethod.ReliableOrdered);
            mEngine.mNetworkMgr.SendClientName(ConfigManager.Singleton.Configuration.PlayerName);
            
            return true;
        }

        public void CreateGUI()
        {
            lobbyChat = new Chatbox("lobbyChat");
            lobbyChat.chatGUI.ZOrder = 10;
            lobbyChat.chatPanel.ResizeMode = Miyagi.UI.ResizeModes.None;
            lobbyChat.chatPanel.Movable = true;
            lobbyChat.TextSubmitted += new Chatbox.TextSubmitHandler(chatTextbox_TextSubmitted);
            
            guiBackground = new GUI("guiBackground");

            Panel guiBackgroundPanel = new Panel("mainGuiBackgroundPanel")
            {
                Location = new Point(0, 0),
                Size = new Size((int)mEngine.Window.Width, (int)mEngine.Window.Height),
                ResizeMode = ResizeModes.None,
                Skin = MiyagiResources.Singleton.Skins["LobbyBackground"],
                AlwaysOnTop = false,
                TextureFiltering = TextureFiltering.Anisotropic
            };
            guiBackground.Controls.Add(guiBackgroundPanel);

            guiBackground.ZOrder = 5;

            guiLobbyMenu = new GUI("guiLobbyMenu");

            Button lobbyJoinGameButton = new Button("lobbyJoinGameButton")
            {
                Location = new Point(580, 350),
                Size = new Size(240, 30),
                Skin = MiyagiResources.Singleton.Skins["ButtonStandardSkin"],
                Text = "Join Game",
                TextStyle =
                {
                    Alignment = Alignment.MiddleCenter,
                    ForegroundColour = Colours.DarkBlue,
                    Font = MiyagiResources.Singleton.Fonts["SpacedockStencil"]
                }
            };
            lobbyJoinGameButton.MouseDown += lobbyJoinGameButtonMouseDown;
            guiLobbyMenu.Controls.Add(lobbyJoinGameButton);
            guiLobbyMenu.ZOrder = 11;

            mEngine.mMiyagiSystem.GUIManager.GUIs.Add(guiBackground);
            mEngine.mMiyagiSystem.GUIManager.GUIs.Add(lobbyChat.chatGUI);
            mEngine.mMiyagiSystem.GUIManager.GUIs.Add(guiLobbyMenu);

            guiBackground.Visible = true;
        }

        void chatTextbox_TextSubmitted(Chatbox chatbox, string text)
        {
            if (text == "/dumpmap")
            {
                /* if(map != null && itemManager != null)
                     MapFileHandler.SaveMap("./Maps/mapdump.map", map, itemManager);*/
            }
            else
            {
                SendLobbyChat(text);
            }
        }

        private void lobbyJoinGameButtonMouseDown(object sender, MouseButtonEventArgs e)
        {
            playerController.SendVerb("joingame", 0);
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
        }

        //public void clientName_OnSubmit(UiTextbox source, string the_text)
        //{
        //    while (the_text.Length < 3)
        //    {
        //        the_text += "_";
        //    }
        //    the_text = the_text.Replace(' ', '_');
        //    the_text = the_text.Trim();
        //    source.SetText(the_text);
        //    mEngine.mNetworkMgr.SendClientName(the_text);
        //}

        //public void playButton_OnPress(UiButton source, MOIS.MouseButtonID button)
        //{
        //    mStateMgr.RequestStateChange(typeof(EditScreen));
        //}

        //public void menuButton_OnPress(UiButton source, MOIS.MouseButtonID button)
        //{
        //    mStateMgr.RequestStateChange(typeof(MainMenu));
        //    mEngine.mNetworkMgr.Disconnect();
        //}

        //public void chatBox_OnSubmit(UiTextbox source, string the_text)
        //{
        //    mEngine.mNetworkMgr.SendLobbyChat(the_text);
        //    source.SetText("");
        //}

        public void SendLobbyChat(string text)
        {
            NetOutgoingMessage message = mEngine.mNetworkMgr.netClient.CreateMessage();
            message.Write((byte)NetMessage.ChatMessage);
            message.Write((byte)ChatChannel.Lobby);
            message.Write(text);

            mEngine.mNetworkMgr.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
        }

        public override void Shutdown()
        {
            mEngine.mNetworkMgr.MessageArrived -= new NetworkMsgHandler(mNetworkMgr_MessageArrived);
        }

        public override void Update(long _frameTime)
        {
        }

        private void HandleWelcomeMessage(NetIncomingMessage msg)
        {
            string serverName = msg.ReadString();
            int serverPort = msg.ReadInt32();
            string welcomeString = msg.ReadString();
            serverMaxPlayers = msg.ReadInt32();
            string serverMapName = msg.ReadString();
            GameType gameType = (GameType)msg.ReadByte();
            lobbyChat.AddLine("Server name: " + serverName + "\r\nMaxPlayers: " + serverMaxPlayers.ToString() + "\r\n" + welcomeString);
        }

        private void HandleChatMessage(NetIncomingMessage msg)
        {
            ChatChannel channel = (ChatChannel)msg.ReadByte();
            string text = msg.ReadString();

            string message = "(" + channel.ToString() + "):" + text;
            ushort atomID = msg.ReadUInt16();
            lobbyChat.AddLine(message);
        }

        #region Input
        public override void UpdateInput(Mogre.FrameEvent evt, MOIS.Keyboard keyState, MOIS.Mouse mouseState)
        {
        }

        public override void KeyDown(MOIS.KeyEvent keyState)
        {
        }

        public override void KeyUp(MOIS.KeyEvent keyState)
        {
        }

        public override void MouseUp(MOIS.MouseEvent mouseState, MOIS.MouseButtonID button)
        {
        }

        public override void MouseDown(MOIS.MouseEvent mouseState, MOIS.MouseButtonID button)
        {
        }

        public override void MouseMove(MOIS.MouseEvent mouseState)
        {
        } 
        #endregion
    }
}
