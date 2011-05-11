using System;

using Mogre;
using Lidgren.Network;

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
        private OgreManager mEngine;
        private StateManager mStateMgr;
        private int serverMaxPlayers;
        private Chatbox lobbyChat;

        public LobbyScreen()
        {
            mEngine = null;
        }

        public override bool Startup(StateManager _mgr)
        {
            mEngine = _mgr.Engine;
            mStateMgr = _mgr;

            mEngine.mNetworkMgr.MessageArrived += new NetworkMsgHandler(mNetworkMgr_MessageArrived);

            lobbyChat = new Chatbox("lobbyChat");
            mEngine.mMiyagiSystem.GUIManager.GUIs.Add(lobbyChat.chatGUI);
            lobbyChat.chatPanel.ResizeMode = Miyagi.UI.ResizeModes.None;
            lobbyChat.chatPanel.Movable = false;

            //guiConnectMenu.Resize(mEngine.ScalarX, mEngine.ScalarY);

            NetworkManager netMgr = mEngine.mNetworkMgr;
            NetOutgoingMessage message = netMgr.netClient.CreateMessage();
            message.Write((byte)NetMessage.WelcomeMessage); //Request Welcome msg.
            netMgr.netClient.SendMessage(message, NetDeliveryMethod.ReliableOrdered);

            return true;
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
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }
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
            NetworkManager netMgr = mEngine.mNetworkMgr;
            NetOutgoingMessage message = netMgr.netClient.CreateMessage();
            message.Write((byte)NetMessage.LobbyChat);
            message.Write(text);
            netMgr.netClient.SendMessage(message, NetDeliveryMethod.ReliableOrdered);
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
            string welcomeString = msg.ReadString();
            serverMaxPlayers = msg.ReadInt32();
            string serverMapNames = msg.ReadString();
            GameType gameType = (GameType)msg.ReadByte();
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
