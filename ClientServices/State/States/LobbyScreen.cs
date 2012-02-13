using System;
using ClientInterfaces.State;
using ClientServices.UserInterface.Components;
using Lidgren.Network;
using System.Collections.Generic;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using SS13_Shared;
using System.Drawing;

namespace ClientServices.State.States
{
    public class LobbyScreen :State, IState
    {
        private int _serverMaxPlayers;
        private string _serverName;
        private int _serverPort;
        private string _welcomeString;
        private string _gameType;
        private string _serverMapName;

        ScrollableContainer _jobButtonContainer;

        private readonly List<String> _playerListStrings = new List<string>();

        private Chatbox _lobbyChat;

        private const double PlayerListRefreshDelaySec = 3; //Time in seconds before refreshing the playerlist.
        private DateTime _playerListTime;

        private TextSprite _lobbyText;

        public LobbyScreen(IDictionary<Type, object> managers)
            : base(managers)
        {
            
        }

        public void Startup()
        {
            PlayerManager.SetState(this);

            UserInterfaceManager.DisposeAllComponents();

            NetworkManager.MessageArrived += NetworkManagerMessageArrived;

            _lobbyChat = new Chatbox(ResourceManager, UserInterfaceManager, KeyBindingManager);
            _lobbyChat.TextSubmitted += LobbyChatTextSubmitted;

            UserInterfaceManager.AddComponent(_lobbyChat);

            _lobbyText = new TextSprite("lobbyText", "", ResourceManager.GetFont("CALIBRI"))
                            {
                                Color = System.Drawing.Color.Black,
                                ShadowColor = System.Drawing.Color.DimGray,
                                Shadowed = true,
                                ShadowOffset = new Vector2D(1, 1)
                            };

            var message = NetworkManager.CreateMessage();
            message.Write((byte)NetMessage.WelcomeMessage); //Request Welcome msg.
            NetworkManager.SendMessage(message, NetDeliveryMethod.ReliableOrdered);

            NetworkManager.SendClientName(ConfigurationManager.GetPlayerName()); //Send name.

            var playerListMsg = NetworkManager.CreateMessage();
            playerListMsg.Write((byte)NetMessage.PlayerList); //Request Playerlist.
            NetworkManager.SendMessage(playerListMsg, NetDeliveryMethod.ReliableOrdered);

            _playerListTime = DateTime.Now.AddSeconds(PlayerListRefreshDelaySec);

            var jobListMsg = NetworkManager.CreateMessage();
            jobListMsg.Write((byte)NetMessage.JobList); //Request Joblist.
            NetworkManager.SendMessage(jobListMsg, NetDeliveryMethod.ReliableOrdered);

            var joinButton = new Button("Join Game", ResourceManager);
            joinButton.mouseOverColor = Color.LightSteelBlue;
            joinButton.Clicked += JoinButtonClicked;
            joinButton.Position = new System.Drawing.Point(605 - joinButton.ClientArea.Width - 5, 200 - joinButton.ClientArea.Height - 5);
            UserInterfaceManager.AddComponent(joinButton);

            _jobButtonContainer = new ScrollableContainer("LobbyJobCont", new System.Drawing.Size(450, 400), ResourceManager)
                                     {
                                         Position = new System.Drawing.Point(630, 10)
                                     };

            UserInterfaceManager.AddComponent(_jobButtonContainer);

            Gorgon.CurrentRenderTarget.Clear();
        }

        void JoinButtonClicked(Button sender)
        {
            PlayerManager.SendVerb("joingame", 0);
        }

        void LobbyChatTextSubmitted(Chatbox chatbox, string text)
        {
            SendLobbyChat(text);
        }

        public void GorgonRender(FrameEventArgs e)
        {
            Gorgon.CurrentRenderTarget.Clear();
            Gorgon.CurrentRenderTarget.FilledRectangle(5, 5, 600, 200, System.Drawing.Color.SlateGray);
            Gorgon.CurrentRenderTarget.FilledRectangle(625, 5, Gorgon.CurrentRenderTarget.Width - 625 - 5, Gorgon.CurrentRenderTarget.Height - 5 - 6, System.Drawing.Color.SlateGray);
            Gorgon.CurrentRenderTarget.FilledRectangle(5, 220, 600, _lobbyChat.Position.Y - 250 - 5, System.Drawing.Color.SlateGray);
            _lobbyText.Position = new Vector2D(10, 10);
            _lobbyText.Text = "Server: " + _serverName;
            _lobbyText.Draw();
            _lobbyText.Position = new Vector2D(10, 30);
            _lobbyText.Text = "Server-Port: "+ _serverPort;
            _lobbyText.Draw();
            _lobbyText.Position = new Vector2D(10, 50);
            _lobbyText.Text = "Max Players: " + _serverMaxPlayers;
            _lobbyText.Draw();
            _lobbyText.Position = new Vector2D(10, 70);
            _lobbyText.Text = "Gamemode: " + _gameType;
            _lobbyText.Draw();
            _lobbyText.Position = new Vector2D(10, 110);
            _lobbyText.Text = "MOTD: \n" + _welcomeString;
            _lobbyText.Draw();

            var pos = 225;
            foreach (var plrStr in _playerListStrings)
            {
                _lobbyText.Position = new Vector2D(10, pos);
                _lobbyText.Text = plrStr;
                _lobbyText.Draw();
                pos += 20;
            }

            UserInterfaceManager.Render();
        }

        public void FormResize()
        {
            UserInterfaceManager.ResizeComponents();
        }

        void NetworkManagerMessageArrived(object sender, IncomingNetworkMessageArgs args)
        {
            var message = args.Message;
            switch (message.MessageType)
            {
                case NetIncomingMessageType.StatusChanged:
                    var statMsg = (NetConnectionStatus)message.ReadByte();
                    if (statMsg == NetConnectionStatus.Disconnected)
                    {
                        var disconnectMessage = message.ReadString();
                        UserInterfaceManager.AddComponent(new DisconnectedScreenBlocker(StateManager, UserInterfaceManager, ResourceManager, disconnectMessage));
                    }
                    break;
                case NetIncomingMessageType.Data:
                    var messageType = (NetMessage)message.ReadByte();
                    switch (messageType)
                    {
                        case NetMessage.LobbyChat:
                            var text = message.ReadString();
                            AddChat(text);
                            break;
                        case NetMessage.PlayerCount:
                            var newCount = message.ReadByte();
                            break;
                        case NetMessage.PlayerList:
                            HandlePlayerList(message);
                            break;
                        case NetMessage.WelcomeMessage:
                            HandleWelcomeMessage(message);
                            break;
                        case NetMessage.ChatMessage:
                            HandleChatMessage(message);
                            break;
                        case NetMessage.JobList:
                            HandleJobList(message);
                            break;
                        case NetMessage.JobSelected:
                            HandleJobSelected(message);
                            break;
                        case NetMessage.JoinGame:
                            HandleJoinGame();
                            break;
                    }
                    break;
            }
        }

        private void HandleJobSelected(NetIncomingMessage msg)
        {
            string jobName = msg.ReadString();
            foreach(GuiComponent comp in _jobButtonContainer.components)
                ((JobSelectButton)comp).Selected = ((JobDefinition)comp.UserData).Name == jobName;
        }

        private void HandleJobList(NetIncomingMessage msg)
        {
            var jobListXml = msg.ReadString(); //READ THE WHOLE XML FILE.
            JobHandler.Singleton.LoadDefinitionsFromString(jobListXml);
            var pos = 5;
            _jobButtonContainer.components.Clear(); //Properly dispose old buttons !!!!!!!
            foreach (var definition in JobHandler.Singleton.JobDefinitions)
            {
                var current = new JobSelectButton(definition.Name, definition.JobIcon, definition.Description, ResourceManager)
                                  {
                                      Available = definition.Available,
                                      Position = new System.Drawing.Point(5, pos)
                                  };

                current.Clicked += CurrentClicked;
                current.UserData = definition;
                _jobButtonContainer.components.Add(current);
                pos += current.ClientArea.Height + 20;
            }
        }

        void CurrentClicked(JobSelectButton sender)
        {
            var playerJobSpawnMsg = NetworkManager.CreateMessage();
            var picked = (JobDefinition)sender.UserData;
            playerJobSpawnMsg.Write((byte)NetMessage.RequestJob); //Request job.
            playerJobSpawnMsg.Write(picked.Name);
            NetworkManager.SendMessage(playerJobSpawnMsg, NetDeliveryMethod.ReliableOrdered);
        }

        private void HandlePlayerList(NetIncomingMessage msg)
        {
            var playerCount = msg.ReadByte();
            _playerListStrings.Clear();
            for (var i = 0; i < playerCount; i++)
            {
                var currName = msg.ReadString();
                var currStatus = (SessionStatus)msg.ReadByte();
                var currRoundtrip = msg.ReadFloat();
                _playerListStrings.Add(currName + "\tStatus: " + currStatus + "\tLatency: " + Math.Truncate(currRoundtrip * 1000) + " ms");
            }
        }

        private void HandleJoinGame()
        {
            StateManager.RequestStateChange<GameScreen>();
        }

        private void AddChat(string text)
        {
            _lobbyChat.AddLine(text, ChatChannel.Lobby);
        }

        public void SendLobbyChat(string text)
        {
            var message = NetworkManager.CreateMessage();
            message.Write((byte)NetMessage.ChatMessage);
            message.Write((byte)ChatChannel.Lobby);
            message.Write(text);

            NetworkManager.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
        }

        public void Shutdown()
        {
            UserInterfaceManager.DisposeAllComponents();
            NetworkManager.MessageArrived -= NetworkManagerMessageArrived;
            RenderTargetCache.DestroyAll();
        }

        public void Update(FrameEventArgs e)
        {
            UserInterfaceManager.Update();
            if (_playerListTime.CompareTo(DateTime.Now) < 0)
            {
                NetOutgoingMessage playerListMsg = NetworkManager.CreateMessage();
                playerListMsg.Write((byte)NetMessage.PlayerList); //Request Playerlist.
                NetworkManager.SendMessage(playerListMsg, NetDeliveryMethod.ReliableOrdered);

                _playerListTime = DateTime.Now.AddSeconds(PlayerListRefreshDelaySec);
            }
        }

        private void HandleWelcomeMessage(NetIncomingMessage msg)
        {
            _serverName = msg.ReadString();
            _serverPort = msg.ReadInt32();
            _welcomeString = msg.ReadString();
            _serverMaxPlayers = msg.ReadInt32();
            _serverMapName = msg.ReadString();
            _gameType = msg.ReadString();
        }

        private void HandleChatMessage(NetIncomingMessage msg)
        {
            var channel = (ChatChannel)msg.ReadByte();
            string text = msg.ReadString();
            string message = "[" + channel + "] " + text;
            _lobbyChat.AddLine(message, ChatChannel.Lobby);
        }

        #region Input
 
        public void KeyDown(KeyboardInputEventArgs e)
        {
            UserInterfaceManager.KeyDown(e);
        }
        public void KeyUp(KeyboardInputEventArgs e)
        {
        }
        public void MouseUp(MouseInputEventArgs e)
        {
            UserInterfaceManager.MouseUp(e);
        }
        public void MouseDown(MouseInputEventArgs e)
        {
            UserInterfaceManager.MouseDown(e);
        }
        public void MouseMove(MouseInputEventArgs e)
        {
            UserInterfaceManager.MouseMove(e);
        }
        public void MouseWheelMove(MouseInputEventArgs e)
        {
            UserInterfaceManager.MouseWheelMove(e);
        }
        #endregion
    }
}
