using System;

using Lidgren.Network;

using SS3D.Modules;
using SS3D.Modules.Network;

using System.Collections.Generic;
using System.Reflection;

using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using SS3D.UserInterface;
using SS3D_shared;
using System.Linq;
using SS3D.UserInterface;
using ClientConfigManager;
using ClientResourceManager;
using ClientServices.Map;

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
        private string gameType;

        List<JobSelectButton> jobButtons = new List<JobSelectButton>();
        ScrollableContainer jobButtonContainer;

        private List<String> PlayerListStrings = new List<string>();


        private PlayerController playerController;
        private Chatbox lobbyChat;

        private const double playerListRefreshDelaySec = 3; //Time in seconds before refreshing the playerlist.
        private DateTime playerListTime = new DateTime();

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

            UiManager.Singleton.DisposeAllComponents();

            prg.mNetworkMgr.MessageArrived += new NetworkMsgHandler(mNetworkMgr_MessageArrived);

            lobbyChat = new SS3D.UserInterface.Chatbox("lobbyChat");
            lobbyChat.TextSubmitted += new SS3D.UserInterface.Chatbox.TextSubmitHandler(lobbyChat_TextSubmitted);

            UiManager.Singleton.Components.Add(lobbyChat);

            lobbyText = new TextSprite("lobbyText", "", ResMgr.Singleton.GetFont("CALIBRI"));
            lobbyText.Color = System.Drawing.Color.Black;
            lobbyText.ShadowColor = System.Drawing.Color.DimGray;
            lobbyText.Shadowed = true;
            lobbyText.ShadowOffset = new Vector2D(1, 1);

            NetworkManager netMgr = prg.mNetworkMgr;
            NetOutgoingMessage message = netMgr.netClient.CreateMessage();
            message.Write((byte)NetMessage.WelcomeMessage); //Request Welcome msg.
            netMgr.netClient.SendMessage(message, NetDeliveryMethod.ReliableOrdered);

            prg.mNetworkMgr.SendClientName(ConfigManager.Singleton.Configuration.PlayerName); //Send name.

            NetOutgoingMessage playerListMsg = netMgr.netClient.CreateMessage();
            playerListMsg.Write((byte)NetMessage.PlayerList); //Request Playerlist.
            netMgr.netClient.SendMessage(playerListMsg, NetDeliveryMethod.ReliableOrdered);

            playerListTime = DateTime.Now.AddSeconds(playerListRefreshDelaySec);

            NetOutgoingMessage jobListMsg = netMgr.netClient.CreateMessage();
            jobListMsg.Write((byte)NetMessage.JobList); //Request Joblist.
            netMgr.netClient.SendMessage(jobListMsg, NetDeliveryMethod.ReliableOrdered);

            Button joinButton = new Button("Join Game");
            joinButton.Clicked += new Button.ButtonPressHandler(joinButt_Clicked);
            joinButton.Position = new System.Drawing.Point(605 - joinButton.ClientArea.Width - 5, 230 - joinButton.ClientArea.Height - 5);
            UiManager.Singleton.Components.Add(joinButton);

            jobButtonContainer = new ScrollableContainer("LobbyJobCont", new System.Drawing.Size(450, 400));
            jobButtonContainer.Position = new System.Drawing.Point(630, 35);
            UiManager.Singleton.Components.Add(jobButtonContainer);

            Gorgon.Screen.Clear();

            //BYPASS LOBBY
            //playerController.SendVerb("joingame", 0);
            return true;
        }

        void joinButt_Clicked(Button sender)
        {
            playerController.SendVerb("joingame", 0);
        }

        void lobbyChat_TextSubmitted(SS3D.UserInterface.Chatbox Chatbox, string Text)
        {
            SendLobbyChat(Text);
        }

        public override void GorgonRender(FrameEventArgs e)
        {
            Gorgon.Screen.Clear();
            Gorgon.Screen.FilledRectangle(5, 30, 600, 200, System.Drawing.Color.SlateGray);
            Gorgon.Screen.FilledRectangle(625, 30, Gorgon.Screen.Width - 625 - 5, Gorgon.Screen.Height - 30 - 6, System.Drawing.Color.SlateGray);
            Gorgon.Screen.FilledRectangle(5, 250, 600, lobbyChat.Position.Y - 250 -25, System.Drawing.Color.SlateGray);
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
            lobbyText.Text = "Gamemode: " + gameType;
            lobbyText.Draw();
            lobbyText.Position = new Vector2D(10, 135);
            lobbyText.Text = "MOTD: \n" + welcomeString;
            lobbyText.Draw();

            int Pos = 255;
            foreach (string plrStr in PlayerListStrings)
            {
                lobbyText.Position = new Vector2D(10, Pos);
                lobbyText.Text = plrStr;
                lobbyText.Draw();
                Pos += 20;
            }

            UiManager.Singleton.Render();

            return;
        }

        public override void FormResize()
        {
            //throw new NotImplementedException();
        }

        void mNetworkMgr_MessageArrived(NetworkManager netMgr, NetIncomingMessage msg)
        {
            switch (msg.MessageType)
            {
                case NetIncomingMessageType.StatusChanged:
                    NetConnectionStatus statMsg = (NetConnectionStatus)msg.ReadByte();
                    if (statMsg == NetConnectionStatus.Disconnected)
                    {
                        string discMsg = msg.ReadString();
                        UiManager.Singleton.Components.Add(new DisconnectedScreenBlocker(mStateMgr, discMsg));
                    }
                    break;
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
                        case NetMessage.PlayerList:
                            HandlePlayerList(msg);
                            break;
                        case NetMessage.WelcomeMessage:
                            HandleWelcomeMessage(msg);
                            break;
                        case NetMessage.ChatMessage:
                            HandleChatMessage(msg);
                            break;
                        case NetMessage.JobList:
                            HandleJobList(msg);
                            break;
                        case NetMessage.JobSelected:
                            HandleJobSelected(msg);
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

        private void HandleJobSelected(NetIncomingMessage msg)
        {
            string jobName = msg.ReadString();
            foreach(GuiComponent comp in jobButtonContainer.components)
                if (((JobDefinition)((JobSelectButton)comp).UserData).Name == jobName) ((JobSelectButton)comp).selected = true; else ((JobSelectButton)comp).selected = false;
            return;
        }

        private void HandleJobList(NetIncomingMessage msg)
        {
            string jobListXML = msg.ReadString(); //READ THE WHOLE XML FILE.
            JobHandler.Singleton.LoadDefinitionsFromString(jobListXML);
            int pos = 5;
            jobButtonContainer.components.Clear(); //Properly dispose old buttons !!!!!!!
            foreach (JobDefinition definition in JobHandler.Singleton.JobDefinitions)
            {
                JobSelectButton current = new JobSelectButton(definition.Name, definition.JobIcon, definition.Description);
                current.available = definition.Available;
                current.Position = new System.Drawing.Point(5, pos);
                current.Clicked += new JobSelectButton.JobButtonPressHandler(current_Clicked);
                current.UserData = definition;
                jobButtonContainer.components.Add(current);
                pos += current.ClientArea.Height + 20;
            }
            return;
        }

        void current_Clicked(JobSelectButton sender)
        {
            NetOutgoingMessage playerJobSpawnMsg = prg.mNetworkMgr.netClient.CreateMessage();
            JobDefinition picked = (JobDefinition)sender.UserData;
            playerJobSpawnMsg.Write((byte)NetMessage.RequestJob); //Request job.
            playerJobSpawnMsg.Write(picked.Name);
            prg.mNetworkMgr.netClient.SendMessage(playerJobSpawnMsg, NetDeliveryMethod.ReliableOrdered);
        }

        private void HandlePlayerList(NetIncomingMessage msg)
        {
            byte playerCount = msg.ReadByte();
            PlayerListStrings.Clear();
            for (int i = 0; i < playerCount; i++)
            {
                string currName = msg.ReadString();
                SessionStatus currStatus = (SessionStatus)msg.ReadByte();
                float currRoundtrip = msg.ReadFloat();
                PlayerListStrings.Add(currName + "     Status: " + currStatus.ToString() + "     Latency: " + Math.Truncate(currRoundtrip * 1000).ToString() + " ms");
            }
            return;
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
            UiManager.Singleton.DisposeAllComponents();
            //UIDesktop.Singleton.Dispose();
            prg.mNetworkMgr.MessageArrived -= new NetworkMsgHandler(mNetworkMgr_MessageArrived);
            RenderTargetCache.DestroyAll();
        }

        public override void Update(FrameEventArgs e)
        {
            UiManager.Singleton.Update();
            if (playerListTime.CompareTo(DateTime.Now) < 0)
            {
                NetOutgoingMessage playerListMsg = prg.mNetworkMgr.netClient.CreateMessage();
                playerListMsg.Write((byte)NetMessage.PlayerList); //Request Playerlist.
                prg.mNetworkMgr.netClient.SendMessage(playerListMsg, NetDeliveryMethod.ReliableOrdered);

                playerListTime = DateTime.Now.AddSeconds(playerListRefreshDelaySec);
            }
        }

        private void HandleWelcomeMessage(NetIncomingMessage msg)
        {
            serverName = msg.ReadString();
            serverPort = msg.ReadInt32();
            welcomeString = msg.ReadString();
            serverMaxPlayers = msg.ReadInt32();
            serverMapName = msg.ReadString();
            gameType = msg.ReadString();
        }

        private void HandleChatMessage(NetIncomingMessage msg)
        {
            ChatChannel channel = (ChatChannel)msg.ReadByte();
            string text = msg.ReadString();
            string message = "(" + channel.ToString() + "):" + text;
            int atomID = msg.ReadInt32();
            lobbyChat.AddLine(message, ChatChannel.Lobby);
        }

        #region Input
 
        public override void KeyDown(KeyboardInputEventArgs e)
        {
            UiManager.Singleton.KeyDown(e);
        }
        public override void KeyUp(KeyboardInputEventArgs e)
        {
        }
        public override void MouseUp(MouseInputEventArgs e)
        {
            UiManager.Singleton.MouseUp(e);
        }
        public override void MouseDown(MouseInputEventArgs e)
        {
            UiManager.Singleton.MouseDown(e);
        }
        public override void MouseMove(MouseInputEventArgs e)
        {
            UiManager.Singleton.MouseMove(e);
        }
        public override void MouseWheelMove(MouseInputEventArgs e)
        {
            UiManager.Singleton.MouseWheelMove(e);
        }
        #endregion
    }
}
