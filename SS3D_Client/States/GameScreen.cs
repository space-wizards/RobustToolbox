using System;
using System.IO;

using Mogre;
using Lidgren.Network;

using SS3D.Modules;
using SS3D.Modules.Map;
using SS3D.Modules.Items;
using SS3D.Modules.Mobs;
using SS3D.Modules.Network;
using SS3D.Modules.UI;

using SS3D_shared;

using System.Collections.Generic;
using System.Reflection;

using Miyagi;
using Miyagi.UI;
using Miyagi.UI.Controls;
using Miyagi.Common;
using Miyagi.Common.Data;
using Miyagi.Common.Resources;
using Miyagi.Common.Events;
using Miyagi.TwoD;

namespace SS3D.States
{
    public class GameScreen : State
    {
        #region Variables
        private OgreManager mEngine;
        private StateManager mStateMgr;
        private Map map;
        private ItemManager itemManager;
        private MobManager mobManager;
        private GUI guiGameScreen;
        private Chatbox gameChat;
        private ushort defaultChannel;


        #region Mouse/Camera stuff
        private DateTime lastRMBClick = DateTime.Now;
        private int lastMouseX = 0;
        private int lastMouseY = 0;
        #endregion

        #endregion

        public GameScreen()
        {
            mEngine = null;
        }

        #region Startup, Shutdown, Update
        public override bool Startup(StateManager _mgr)
        {
            mEngine = _mgr.Engine;
            mStateMgr = _mgr;

            mEngine.mMiyagiSystem.GUIManager.DisposeAllGUIs();

            map = new Map(mEngine);

            mobManager = new MobManager(mEngine, map, mEngine.mNetworkMgr);
            itemManager = new ItemManager(mEngine, map, mEngine.mNetworkMgr, mobManager);
            SetUp();

            mEngine.mNetworkMgr.MessageArrived += new NetworkMsgHandler(mNetworkMgr_MessageArrived);

            gameChat = new Chatbox("gameChat");
            mEngine.mMiyagiSystem.GUIManager.GUIs.Add(gameChat.chatGUI);
            gameChat.chatPanel.ResizeMode = Miyagi.UI.ResizeModes.None;
            gameChat.chatPanel.Movable = false;
            defaultChannel = 1; 
            gameChat.chatTextbox.Submit += new EventHandler<ValueEventArgs<string>>(chatTextbox_Submit);
            gameChat.chatTextbox.Submit -= new EventHandler<ValueEventArgs<string>>(gameChat.chatTextbox_Submit);


            mEngine.mNetworkMgr.SetMap(map);
            mEngine.mNetworkMgr.RequestMap();


            return true;
        }



        private void SetUp()
        {
            mEngine.SceneMgr.ShadowTextureSelfShadow = true;
            mEngine.SceneMgr.SetShadowTextureCasterMaterial("shadow_caster");
            mEngine.SceneMgr.SetShadowTexturePixelFormat(PixelFormat.PF_FLOAT16_RGB);
            mEngine.SceneMgr.ShadowCasterRenderBackFaces = false;
            mEngine.SceneMgr.SetShadowTextureSize(512);
            mEngine.SceneMgr.ShadowTechnique = ShadowTechnique.SHADOWTYPE_TEXTURE_ADDITIVE_INTEGRATED;
            
            mEngine.SceneMgr.AmbientLight = ColourValue.White;

            mEngine.SceneMgr.SetSkyBox(true, "SkyBox", 900f, true);

        }

        public override void Shutdown()
        {
            mEngine.mMiyagiSystem.GUIManager.GUIs.Remove(guiGameScreen);
            map.Shutdown();
            map = null;
            itemManager.Shutdown();
            itemManager = null;
            mobManager.Shutdown();
            mobManager = null;
            mEngine.mNetworkMgr.Disconnect();
        }

        public override void Update(long _frameTime)
        {
            mEngine.SceneMgr.SkyBoxNode.Rotate(Mogre.Vector3.UNIT_Y, 0.0001f);
            itemManager.Update();
            mobManager.Update();
        }

        private void mNetworkMgr_MessageArrived(NetworkManager netMgr, NetIncomingMessage msg)
        {
            if (msg == null)
            {
                return;
            }
            switch (msg.MessageType)
            {
                case NetIncomingMessageType.Data:
                    NetMessage messageType = (NetMessage)msg.ReadByte();
                    switch (messageType)
                    {
                        case NetMessage.ChangeTile:
                            ChangeTile(msg);
                            break;
                        case NetMessage.ItemMessage:
                            itemManager.HandleNetworkMessage(msg);
                            break;
                        case NetMessage.MobMessage:
                            mobManager.HandleNetworkMessage(msg);
                            break;
                        case NetMessage.SendMap:
                            RecieveMap(msg);
                            break;
                        case NetMessage.ChatMessage:
                            HandleChatMessage(msg);
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }
        }

        public void RecieveMap(NetIncomingMessage msg)
        {
            int mapWidth = msg.ReadInt32();
            int mapHeight = msg.ReadInt32();

            TileType[,] tileArray = new TileType[mapWidth, mapHeight];

            for (int x = 0; x < mapWidth; x++)
            {
                for (int z = 0; z < mapHeight; z++)
                {
                    tileArray[x, z] = (TileType)msg.ReadByte();
                }
            }
            map.LoadNetworkedMap(tileArray, mapWidth, mapHeight);
        }

        private void ChangeTile(NetIncomingMessage msg)
        {
            if (map == null)
            {
                return;
            }
            int x = msg.ReadInt32();
            int z = msg.ReadInt32();
            TileType newTile = (TileType)msg.ReadByte();
            map.ChangeTile(x, z, newTile);
        }

        private void HandleChatMessage(NetIncomingMessage msg)
        {
            ushort channel = msg.ReadUInt16();
            string text = msg.ReadString();

            string message = "(" + channel.ToString() + "):" + text;

            gameChat.AddLine(message);
        }

        private void SendChatMessage(string text)
        {
            NetOutgoingMessage message = mEngine.mNetworkMgr.netClient.CreateMessage();
            message.Write((byte)NetMessage.ChatMessage);
            message.Write(defaultChannel);
            message.Write(text);

            mEngine.mNetworkMgr.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
        }

        void chatTextbox_Submit(object sender, ValueEventArgs<string> e)
        {
            SendChatMessage(e.Data);
        }

        #endregion

        #region Input
        public override void UpdateInput(Mogre.FrameEvent evt, MOIS.Keyboard keyState, MOIS.Mouse mouseState)
        {
            if(keyState.IsKeyDown(MOIS.KeyCode.KC_W))
            {
                mobManager.MoveMe(1);
                mobManager.Animate("walk");
            }
            if(keyState.IsKeyDown(MOIS.KeyCode.KC_D))
            {
                mobManager.MoveMe(2);
            }
            if (keyState.IsKeyDown(MOIS.KeyCode.KC_A))
            {
                mobManager.MoveMe(3);
            }
            if (keyState.IsKeyDown(MOIS.KeyCode.KC_S))
            {
                mobManager.MoveMe(4);
                mobManager.Animate("walk");
            }
            if (!keyState.IsKeyDown(MOIS.KeyCode.KC_W) && !keyState.IsKeyDown(MOIS.KeyCode.KC_S))
            {
                mobManager.Animate("idle");
            }
        }

        public override void KeyDown(MOIS.KeyEvent keyState)
        {
            if (keyState.key == MOIS.KeyCode.KC_LSHIFT)
            {
                mobManager.myMob.speed = mobManager.myMob.runSpeed;
            }
        }

        public override void KeyUp(MOIS.KeyEvent keyState)
        {
            if (keyState.key == MOIS.KeyCode.KC_LSHIFT)
            {
                mobManager.myMob.speed = mobManager.myMob.walkSpeed;
            }
        }

        public override void MouseUp(MOIS.MouseEvent mouseState, MOIS.MouseButtonID button)
        {
        }

        public override void MouseDown(MOIS.MouseEvent mouseState, MOIS.MouseButtonID button)
        {
            if (button == MOIS.MouseButtonID.MB_Right)
            {
                TimeSpan clickDiff = DateTime.Now - lastRMBClick;
                lastRMBClick = DateTime.Now;
                if (clickDiff.TotalMilliseconds < 250)
                {
                    mEngine.Camera.ParentNode.ResetOrientation();
                }
            }

            if (button == MOIS.MouseButtonID.MB_Left)
            {
                Mogre.Vector3 worldPos;
                Point mousePos = mEngine.mMiyagiSystem.InputManager.MouseLocation;
                Mogre.Vector2 mousePosAbs = new Vector2((float)mousePos.X / (float)mEngine.Window.Width, (float)mousePos.Y / (float)mEngine.Window.Height);
                AtomBaseClass atom = HelperClasses.AtomUtil.PickAtScreenPosition(mEngine, mousePosAbs, out worldPos);

                if (atom != null && atom.AtomType == AtomType.Item)
                {
                    itemManager.ClickItem((Item)atom);
                }
            }
        }

        public override void MouseMove(MOIS.MouseEvent mouseState)
        {
            if (mouseState.state.ButtonDown(MOIS.MouseButtonID.MB_Right))
            {
                int degree;
                if (mouseState.state.X.rel > lastMouseX)
                {
                    degree = -mouseState.state.X.rel;
                }
                else
                {
                    degree = mouseState.state.X.rel;
                }
                mEngine.Camera.ParentNode.Yaw(Mogre.Math.DegreesToRadians(degree), Node.TransformSpace.TS_WORLD);
                lastMouseX = mouseState.state.X.abs;
            }
        }

        #endregion

    }

}
