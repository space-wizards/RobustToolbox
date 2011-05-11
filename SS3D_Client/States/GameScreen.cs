using System;
using System.IO;

using Mogre;
using Lidgren.Network;

using SS3D.Modules;
using SS3D.Modules.Map;
using SS3D.Modules.Items;
using SS3D.Modules.Mobs;
using SS3D.Modules.Network;

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

            map = new Map(mEngine);
            itemManager = new ItemManager(mEngine, map, mEngine.mNetworkMgr);
            mobManager = new MobManager(mEngine, map, mEngine.mNetworkMgr);
            
            mEngine.Camera.Position = new Mogre.Vector3(0, 300, 0);
            mEngine.Camera.LookAt(new Mogre.Vector3(160,64,160));

            SetUp();

            mEngine.mNetworkMgr.MessageArrived += new NetworkMsgHandler(mNetworkMgr_MessageArrived);

            mEngine.mNetworkMgr.SetMap(map);
            mEngine.mNetworkMgr.RequestMap();

            mEngine.mMiyagiSystem.GUIManager.DisposeAllGUIs();

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

        #endregion

        #region Input
        public override void UpdateInput(Mogre.FrameEvent evt, MOIS.Keyboard keyState, MOIS.Mouse mouseState)
        {
            if(keyState.IsKeyDown(MOIS.KeyCode.KC_W))
            {
                mobManager.MoveMe(1);
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
            }
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
