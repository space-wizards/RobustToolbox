using System;
using System.IO;
using System.Linq;

using Lidgren.Network;

using SS3D.Modules;
using SS3D.Modules.Map;
using SS3D.Modules.Network;
using SS3D.Modules.UI;
using SS3D.Modules.UI.Components;
using SS3D.Atom;
using SS3D.Effects;

using SS3D_shared;

using System.Collections.Generic;
using System.Reflection;

using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

using System.Windows.Forms;
using CGO;

namespace SS3D.States
{
    public class GameScreen : State
    {
        #region Variables
        private StateManager mStateMgr;
        public Map map;
        private AtomManager atomManager;
        private EntityManager entityManager;

        //UI Vars
        #region UI Variables
        private Chatbox gameChat;
        #endregion 

        public PlayerController playerController;
        public DateTime lastUpdate;
        public DateTime now;
        private RenderImage baseTarget;
        private RenderImage lightTarget;
        private RenderImage lightTargetIntermediate;
        private Sprite baseTargetSprite;
        private Sprite lightTargetSprite;
        private Sprite lightTargetIntermediateSprite;
        private Batch gasBatch;
        private Batch wallTopsBatch;
        private Batch decalBatch;
        private Batch lightMapBatch;
        private GaussianBlur gaussianBlur;
        public bool blendLightMap = true;
        
        private List<Light> lightsLastFrame = new List<Light>();
        private List<Light> lightsThisFrame = new List<Light>();

        public int screenWidthTiles = 15; // How many tiles around us do we draw?
        public int screenHeightTiles = 12;

        private float realScreenWidthTiles = 0;
        private float realScreenHeightTiles = 0;

        private bool showDebug = false;     // show AABBs & Bounding Circles on atoms.
        private bool telepathy = false;     // disable visiblity bounds if true

        public float xTopLeft { get; private set; }
        public float yTopLeft { get; private set; }

        private float scaleX = 1.0f;
        private float scaleY = 1.0f;

        private System.Drawing.Point screenSize;
        public string spawnType = "";
        private bool editMode = false;
   
        #region Mouse/Camera stuff
        private DateTime lastRMBClick = DateTime.Now;

        public Vector2D mousePosScreen = Vector2D.Zero;
        public Vector2D mousePosWorld = Vector2D.Zero;

        #endregion

        #endregion

        public GameScreen()
        {
        }

        #region Startup, Shutdown, Update
        public override bool Startup(Program _prg)
        {
            prg = _prg;
            mStateMgr = prg.mStateMgr;

            lastUpdate = DateTime.Now;
            now = DateTime.Now;

            map = new Map();

            UiManager.Singleton.DisposeAllComponents();

            entityManager = new EntityManager(prg.mNetworkMgr.netClient);
            atomManager = new AtomManager(this, prg, entityManager);
            PlayerController.Initialize(this, atomManager);
            playerController = PlayerController.Singleton;

            prg.mNetworkMgr.MessageArrived += new NetworkMsgHandler(mNetworkMgr_MessageArrived);
            prg.mNetworkMgr.Disconnected += new NetworkStateHandler(mNetworkMgr_Disconnected);

            prg.mNetworkMgr.SetMap(map);
            prg.mNetworkMgr.RequestMap();

            //Hide the menu!
            prg.GorgonForm.MainMenuStrip.Hide();

            //TODO This should go somewhere else, there should be explicit session setup and teardown at some point.
            prg.mNetworkMgr.SendClientName(ConfigManager.Singleton.Configuration.PlayerName);

            baseTarget = new RenderImage("baseTarget", Gorgon.Screen.Width, Gorgon.Screen.Height, ImageBufferFormats.BufferRGB888A8);
            
            baseTargetSprite = new Sprite("baseTargetSprite", baseTarget);
            baseTargetSprite.DepthWriteEnabled = false;

            lightTarget = new RenderImage("lightTarget", Gorgon.Screen.Width, Gorgon.Screen.Height, ImageBufferFormats.BufferRGB888A8);
            lightTargetSprite = new Sprite("lightTargetSprite", lightTarget);
            lightTargetSprite.DepthWriteEnabled = false;
            lightTargetIntermediate = new RenderImage("lightTargetIntermediate", Gorgon.Screen.Width, Gorgon.Screen.Height, ImageBufferFormats.BufferRGB888A8);
            lightTargetIntermediateSprite = new Sprite("lightTargetIntermediateSprite", lightTargetIntermediate);
            lightTargetIntermediateSprite.DepthWriteEnabled = false;

            gasBatch = new Batch("gasBatch", 1);
            wallTopsBatch = new Batch("wallTopsBatch", 1);
            decalBatch = new Batch("decalBatch", 1);
            lightMapBatch = new Batch("lightMapBatch", 1);

            gaussianBlur = new GaussianBlur();
            
            realScreenWidthTiles = (float)Gorgon.CurrentClippingViewport.Width / map.tileSpacing;
            realScreenHeightTiles = (float)Gorgon.CurrentClippingViewport.Height / map.tileSpacing;

            screenSize = new System.Drawing.Point(Gorgon.CurrentClippingViewport.Width, Gorgon.CurrentClippingViewport.Height);

            //scaleX = (float)Gorgon.CurrentClippingViewport.Width / (realScreenWidthTiles * map.tileSpacing);
            //scaleY = (float)Gorgon.CurrentClippingViewport.Height / (realScreenHeightTiles * map.tileSpacing);

            PlacementManager.Singleton.Initialize(map, atomManager, this, prg.mNetworkMgr);

            //Init GUI components
            gameChat = new Chatbox("gameChat");
            gameChat.TextSubmitted += new Chatbox.TextSubmitHandler(chatTextbox_TextSubmitted);

            UiManager.Singleton.Components.Add(new HumanInventory(playerController));
            UiManager.Singleton.Components.Add(new HumanHandsGui(playerController));
            UiManager.Singleton.Components.Add(new StatPanelComponent(playerController));

            var appendagesTemp = UiManager.Singleton.GetSingleComponentByGuiComponentType(GuiComponentType.AppendagesComponent); //Better safe than sorry.
            if (appendagesTemp != null) appendagesTemp.Position = new System.Drawing.Point(Gorgon.Screen.Width - 190, Gorgon.Screen.Height - 99);

            HumanInventory invTemp = (HumanInventory)UiManager.Singleton.GetSingleComponentByGuiComponentType(GuiComponentType.HumanInventory); // ugh ugh ugh
            if(invTemp != null) invTemp.SetHandsGUI((HumanHandsGui)UiManager.Singleton.GetSingleComponentByGuiComponentType(GuiComponentType.AppendagesComponent)); // ugh ugh ugh ugh ugh
            
            return true;
        }

        void mNetworkMgr_Disconnected(NetworkManager netMgr)
        {
            mStateMgr.RequestStateChange(typeof(ConnectMenu)); //Fix this. Only temporary solution.
        }

        public override void Shutdown()
        {
            if (baseTarget != null && Gorgon.IsInitialized)
            {
                baseTarget.ForceRelease();
                baseTarget.Dispose();
            }
            if (baseTargetSprite != null && Gorgon.IsInitialized)
            {
                baseTargetSprite.Image = null;
                baseTargetSprite = null;
            }
            if (lightTarget != null && Gorgon.IsInitialized)
            {
                lightTarget.ForceRelease();
                lightTarget.Dispose();
            }
            if (lightTargetSprite != null && Gorgon.IsInitialized)
            {
                lightTargetSprite.Image = null;
                lightTargetSprite = null;
            }
            if (lightTargetIntermediate != null && Gorgon.IsInitialized)
            {
                lightTargetIntermediate.ForceRelease();
                lightTargetIntermediate.Dispose();
            }
            if (lightTargetIntermediateSprite != null && Gorgon.IsInitialized)
            {
                lightTargetIntermediateSprite.Image = null;
                lightTargetIntermediateSprite = null;
            }
            gaussianBlur.Dispose();
            atomManager.Shutdown();
            entityManager.Shutdown();
            map.Shutdown();
            PlacementManager.Singleton.Reset();
            atomManager = null;
            entityManager = null;
            map = null;
            UIDesktop.Singleton.Windows.Remove(gameChat);
            gameChat.Dispose();
            gameChat = null;
            UiManager.Singleton.DisposeAllComponents(); //HerpDerp. This is probably bad. Should not remove them ALL.
            UIDesktop.Singleton.Dispose();
            prg.mNetworkMgr.MessageArrived -= new NetworkMsgHandler(mNetworkMgr_MessageArrived);
            RenderTargetCache.DestroyAll();
            GC.Collect();
        }

        public override void Update( FrameEventArgs e )
        {
            lastUpdate = now;
            now = DateTime.Now;
            atomManager.Update();
            CGO.ComponentManager.Singleton.Update(e.FrameDeltaTime);
            editMode = prg.GorgonForm.editMode;
            PlacementManager.Singleton.Update();
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
                        case NetMessage.MapMessage:
                            map.HandleNetworkMessage(msg);
                            break;
                        case NetMessage.AtmosDisplayUpdate:
                            map.HandleAtmosDisplayUpdate(msg);
                            break;
                        case NetMessage.AtomManagerMessage:
                            atomManager.HandleNetworkMessage(msg);
                            break;
                        case NetMessage.PlayerSessionMessage:
                            playerController.HandleNetworkMessage(msg);
                            break;
                        case NetMessage.PlayerUiMessage:
                            UiManager.Singleton.HandleNetMessage(msg);
                            break;
                        case NetMessage.PlacementManagerMessage:
                            PlacementManager.Singleton.HandleNetMessage(msg);
                            break;
                        case NetMessage.SendMap:
                            RecieveMap(msg);
                            break;
                        case NetMessage.ChatMessage:
                            HandleChatMessage(msg);
                            break;
                        case NetMessage.EntityMessage:
                            entityManager.HandleEntityNetworkMessage(msg);
                            break;
                        case NetMessage.EntityManagerMessage:
                            entityManager.HandleNetworkMessage(msg);
                            break;
                        case NetMessage.RequestAdminLogin:
                            HandleAdminMessage(messageType, msg);
                            break;
                        case NetMessage.RequestAdminPlayerlist:
                            HandleAdminMessage(messageType, msg);
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }
        }

        public void HandleAdminMessage(NetMessage adminMsgType, NetIncomingMessage messageBody)
        {
            switch (adminMsgType)
            {
                case NetMessage.RequestAdminLogin:
                    var openPwWindows = (from GuiComponent component in UiManager.Singleton.Components //Get rid of all curently open login windows.
                                        where component.GetType() == typeof(AdminPasswordDialog)
                                        select component).ToArray(); //ToArray to avoid collection changed exception.

                    foreach (GuiComponent curr in openPwWindows) curr.Dispose();

                    UiManager.Singleton.Components.Add(new AdminPasswordDialog(new System.Drawing.Size(200, 50), prg.mNetworkMgr)); //Create a new one.
                    break;
                case NetMessage.RequestAdminPlayerlist:
                    var openAdminPlListWindows = (from GuiComponent component in UiManager.Singleton.Components //See above.
                                        where component.GetType() == typeof(AdminPlayerPanel)
                                        select component).ToArray();

                    foreach (GuiComponent curr in openAdminPlListWindows) curr.Dispose();

                    UiManager.Singleton.Components.Add(new AdminPlayerPanel(new System.Drawing.Size(600,200), prg.mNetworkMgr, messageBody));
                    break;
            }
        }

        public void RecieveMap(NetIncomingMessage msg)
        {
            int mapWidth = msg.ReadInt32();
            int mapHeight = msg.ReadInt32();

            TileType[,] tileArray = new TileType[mapWidth, mapHeight];
            TileState[,] tileStates = new TileState[mapWidth, mapHeight];

            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    tileArray[x, y] = (TileType)msg.ReadByte();
                    tileStates[x, y] = (TileState)msg.ReadByte();
                }
            }
            map.LoadNetworkedMap(tileArray, tileStates, mapWidth, mapHeight);
        }

        #endregion

        private void HandleChatMessage(NetIncomingMessage msg)
        {
            ChatChannel channel = (ChatChannel)msg.ReadByte();
            string text = msg.ReadString();

            string message = "(" + channel.ToString() + "):" + text;
            int atomID = msg.ReadInt32();
            gameChat.AddLine(message, channel);
            Atom.Atom a = atomManager.GetAtom(atomID);
            if (a != null)
            {
                if (a.speechBubble == null) a.speechBubble = new SpeechBubble(a.name + a.Uid.ToString());
                if(channel == ChatChannel.Ingame || channel == ChatChannel.Player || channel == ChatChannel.Radio)
                    a.speechBubble.SetText(text);
            }
        }

        void chatTextbox_TextSubmitted(Chatbox chatbox, string text)
        {
            SendChatMessage(text);
        }

        private void SendChatMessage(string text)
        {
            NetOutgoingMessage message = prg.mNetworkMgr.netClient.CreateMessage();
            message.Write((byte)NetMessage.ChatMessage);
            message.Write((byte)ChatChannel.Player);
            message.Write(text);

            prg.mNetworkMgr.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
        }

        /* What are we doing here exactly? Well:
         * First we get the tile we are stood on, and try and make this the centre of the view. However if we're too close to one edge
         * we allow us to be drawn nearer that edge, and not in the middle of the screen.
         * We then find how far "into" the map we are (xTopLeft, yTopLeft), the position of the top left of the screen in WORLD
         * co-ordinates so we can work out what we need to draw, and what we dont need to (what's off screen).
         * Then we see if we've moved a tile recently or a flag has been set on the map that we need to update the visibility (a door 
         * opened for example).
         * We then loop through all the tiles, and draw the floor and the sides of the walls, as they will always be under us
         * and the atoms. Next we find all the atoms in view and draw them. Lastly we draw the top section of walls as they will
         * always be on top of us and atoms.
         * */
        public override void GorgonRender(FrameEventArgs e)
        {
            Gorgon.CurrentRenderTarget = baseTarget;

            baseTarget.Clear(System.Drawing.Color.Black);
            lightTarget.Clear(System.Drawing.Color.Black);
            lightTargetIntermediate.Clear(System.Drawing.Color.FromArgb(0,System.Drawing.Color.Black));
            Gorgon.Screen.Clear(System.Drawing.Color.Black);

            Gorgon.Screen.DefaultView.Left = 400;
            Gorgon.Screen.DefaultView.Top = 400;
            if (playerController.controlledAtom != null)
            {
                
                System.Drawing.Point centerTile = map.GetTileArrayPositionFromWorldPosition(playerController.controlledAtom.position);
              
                int xStart = System.Math.Max(0, centerTile.X - (screenWidthTiles / 2) - 1);
                int yStart = System.Math.Max(0, centerTile.Y - (screenHeightTiles / 2) - 1);
                int xEnd = System.Math.Min(xStart + screenWidthTiles + 2, map.mapWidth - 1);
                int yEnd = System.Math.Min(yStart + screenHeightTiles + 2, map.mapHeight - 1);

                xTopLeft = Math.Max(0, playerController.controlledAtom.position.X - ((screenWidthTiles / 2) * map.tileSpacing));
                yTopLeft = Math.Max(0, playerController.controlledAtom.position.Y - ((screenHeightTiles / 2) * map.tileSpacing));
                ///COMPUTE TILE VISIBILITY
                if ((centerTile != map.lastVisPoint || map.needVisUpdate))
                {
                    if (!telepathy)
                    {
                        map.compute_visibility(centerTile.X, centerTile.Y);
                        map.lastVisPoint = centerTile;
                    }
                    else
                    {
                        map.set_all_visible();
                    }
                }


                Tiles.Tile t;

                ///RENDER TILE BASES, PUT GAS SPRITES AND WALL TOP SPRITES INTO BATCHES TO RENDER LATER

                for (int x = xStart; x <= xEnd; x++)
                {
                    for (int y = yStart; y <= yEnd; y++)
                    {
                        t = map.tileArray[x, y];
                        if (!t.Visible)
                            continue;
                        if (t.tileType == TileType.Wall)
                        {
                            if (t.tilePosition.Y <= centerTile.Y)
                            {
                                t.Render(xTopLeft, yTopLeft, map.tileSpacing);
                                t.DrawDecals(xTopLeft, yTopLeft, map.tileSpacing, decalBatch);
                                t.RenderLight(xTopLeft, yTopLeft, map.tileSpacing, lightMapBatch);
                            }
                        }
                        else
                        {
                            t.Render(xTopLeft, yTopLeft, map.tileSpacing);
                            t.DrawDecals(xTopLeft, yTopLeft, map.tileSpacing, decalBatch);
                            t.RenderLight(xTopLeft, yTopLeft, map.tileSpacing, lightMapBatch);
                        }

                        ///Render gas sprites to gas batch
                        t.RenderGas(xTopLeft, yTopLeft, map.tileSpacing, gasBatch);
                        ///Render wall top sprites to wall top batch
                        t.RenderTop(xTopLeft, yTopLeft, map.tileSpacing, wallTopsBatch);
                    }
                }

                Gorgon.CurrentRenderTarget = lightTarget;
                if(lightMapBatch.Count > 0)
                    lightMapBatch.Draw();
                lightMapBatch.Clear();
                Gorgon.CurrentRenderTarget = baseTarget;

                ///Render decal batch
                if (decalBatch.Count > 0)
                    decalBatch.Draw();
                decalBatch.Clear();

                lightsThisFrame.Clear();
                
                ///RENDER ATOMS
                if (atomManager != null)
                {
                    IEnumerable<Atom.Atom> atoms = from a in atomManager.atomDictionary.Values
                                                   where
                                                   a.visible &&
                                                   a.position.X / map.tileSpacing >= xStart &&
                                                   a.position.X / map.tileSpacing <= xEnd &&
                                                   a.position.Y / map.tileSpacing >= yStart &&
                                                   a.position.Y / map.tileSpacing <= yEnd
                                                   orderby a.position.Y + ((a.sprite.Height * a.sprite.UniformScale) / 2) ascending
                                                   orderby a.drawDepth ascending
                                                   select a;

                    foreach (Atom.Atom a in atoms.ToList())
                    {
                        a.Render(xTopLeft, yTopLeft);

                        if (showDebug)
                        {
                            Gorgon.Screen.Circle(a.sprite.BoundingCircle.Center.X, a.sprite.BoundingCircle.Center.Y, a.sprite.BoundingCircle.Radius, System.Drawing.Color.Orange);
                            Gorgon.Screen.Rectangle(a.sprite.AABB.X, a.sprite.AABB.Y, a.sprite.AABB.Width, a.sprite.AABB.Height, System.Drawing.Color.Blue);
                        }

                    }
                
                    //Render Light glows
                    atoms = from a in atomManager.atomDictionary.Values
                                                   where
                                                   a.visible &&
                                                   a.position.X / map.tileSpacing >= xStart &&
                                                   a.position.X / map.tileSpacing <= xEnd &&
                                                   a.position.Y / map.tileSpacing >= yStart &&
                                                   a.position.Y / map.tileSpacing <= yEnd &&
                                                   a.GetType().Name == "WallLight"                                                        
                                                   orderby a.position.Y + ((a.sprite.Height * a.sprite.UniformScale) / 2) ascending
                                                   orderby a.drawDepth ascending
                                                   select a;

                    Gorgon.CurrentRenderTarget = lightTarget;
                    Gorgon.CurrentShader = ResMgr.Singleton.GetShader("Blur");
                    ResMgr.Singleton.GetShader("Blur").Parameters["blurAmount"].SetValue(3.0f);
                    foreach (Atom.Atom a in atoms.ToList())
                    {
                        a.sprite.BlendingMode = BlendingModes.Additive;
                        a.Render(xTopLeft, yTopLeft);
                        a.sprite.BlendingMode = BlendingModes.None;
                    }
                    Gorgon.CurrentShader = null;
                    Gorgon.CurrentRenderTarget = baseTarget;
                }



                ///Render gas batch
                if (gasBatch.Count > 0)
                    gasBatch.Draw();
                gasBatch.Clear();

                ///Render wall tops batch
                if (wallTopsBatch.Count > 0)
                    wallTopsBatch.Draw();
                wallTopsBatch.Clear();
                
                
                ///RENDER GHOSTS
                ///Render person ghosts to have them appear behind walls. This should really be 
                ///better thought out I think, but for now this works...
                /*if (atomManager != null)
                {
                    IEnumerable<Atom.Atom> atoms = from a in atomManager.atomDictionary.Values
                                                   where
                                                   a.IsChildOfType(typeof(Atom.Mob.Mob)) &&
                                                   a.visible &&
                                                   System.Math.Sqrt((playerController.controlledAtom.position.X - a.position.X) * (playerController.controlledAtom.position.X - a.position.X)) < screenHeightTiles * map.tileSpacing + 160 &&
                                                   System.Math.Sqrt((playerController.controlledAtom.position.Y - a.position.Y) * (playerController.controlledAtom.position.Y - a.position.Y)) < screenHeightTiles * map.tileSpacing + 160
                                                   orderby a.position.Y + ((a.sprite.Height * a.sprite.UniformScale) / 2) ascending
                                                   select a;

                    foreach (Atom.Atom a in atoms.ToList())
                    {
                        a.Render(xTopLeft, yTopLeft, 70);
                    }
                }*/

                PlacementManager.Singleton.Draw();
            }

            lightTargetSprite.DestinationBlend = AlphaBlendOperation.Zero;
            lightTargetSprite.SourceBlend = AlphaBlendOperation.One;

            gaussianBlur.SetSize(256.0f);
            gaussianBlur.PerformGaussianBlur(lightTargetSprite, lightTarget);
            gaussianBlur.SetSize(512.0f);
            gaussianBlur.PerformGaussianBlur(lightTargetSprite, lightTarget);
            gaussianBlur.SetSize(1024.0f);
            gaussianBlur.PerformGaussianBlur(lightTargetSprite, lightTarget);
            
            baseTargetSprite.Draw();

            if (blendLightMap)
            {
                lightTargetSprite.DestinationBlend = AlphaBlendOperation.InverseSourceAlpha; // Use the alpha of the light to do bright/darkness
                lightTargetSprite.SourceBlend = AlphaBlendOperation.DestinationColor;
            }
            else
            {
                lightTargetSprite.DestinationBlend = AlphaBlendOperation.Zero; // Use the alpha of the light to do bright/darkness
                lightTargetSprite.SourceBlend = AlphaBlendOperation.One;
            }
            lightTargetSprite.Draw();

            Gorgon.CurrentRenderTarget = null;
            //baseTargetSprite.Draw();
            
            return;
        }

        // Not currently used.
        public override void FormResize()
        {
            scaleX = (float)Gorgon.CurrentClippingViewport.Width / (realScreenWidthTiles * map.tileSpacing);
            scaleY = (float)Gorgon.CurrentClippingViewport.Height / (realScreenHeightTiles * map.tileSpacing);
            screenSize = new System.Drawing.Point(Gorgon.CurrentClippingViewport.Width, Gorgon.CurrentClippingViewport.Height);
        }

        #region Input

        public override void KeyDown(KeyboardInputEventArgs e)
        {
            if (gameChat.Active)
                return;

            if (UiManager.Singleton.KeyDown(e)) //KeyDown returns true if the click is handled by the ui component.
                return;

            if (e.Key == KeyboardKeys.F9)
            {
                if (prg.GorgonForm.MainMenuStrip.Visible)
                {
                    prg.GorgonForm.MainMenuStrip.Hide();
                    prg.GorgonForm.MainMenuStrip.Visible = false;
                }
                else
                {
                    prg.GorgonForm.MainMenuStrip.Show();
                    prg.GorgonForm.MainMenuStrip.Visible = true;
                }
                    
            }
            if (e.Key == KeyboardKeys.F1)
            {
                Gorgon.FrameStatsVisible = !Gorgon.FrameStatsVisible;
            }
            if (e.Key == KeyboardKeys.F2)
            {
                showDebug = !showDebug;
            }
            if (e.Key == KeyboardKeys.F3)
            {
                playerController.SendVerb("toxins", 0);
            }

            if (e.Key == KeyboardKeys.F5)
            {
                playerController.SendVerb("save", 0);
            }
            if (e.Key == KeyboardKeys.F6)
            {
                telepathy = !telepathy;
            }
            if (e.Key == KeyboardKeys.F7)
            {
                blendLightMap = !blendLightMap;
            }

            if (e.Key == KeyboardKeys.F8)
            {
                NetOutgoingMessage message = prg.mNetworkMgr.netClient.CreateMessage();
                message.Write((byte)NetMessage.ForceRestart);
                prg.mNetworkMgr.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
            }

            if (e.Key == KeyboardKeys.F12)
            {
                NetOutgoingMessage message = prg.mNetworkMgr.netClient.CreateMessage();
                message.Write((byte)NetMessage.RequestAdminPlayerlist);
                prg.mNetworkMgr.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
            }

            playerController.KeyDown(e.Key);
        }

        public override void KeyUp(KeyboardInputEventArgs e)
        {
            playerController.KeyUp(e.Key);
        }
        public override void MouseUp(MouseInputEventArgs e)
        {
            if (UiManager.Singleton.MouseUp(e)) //Returns True if a component handled the event.
                return;
        }
        public override void MouseDown(MouseInputEventArgs e)
        {
            if (PlacementManager.Singleton.active != null)
            {
                if (e.Buttons == GorgonLibrary.InputDevices.MouseButtons.Left)
                {
                    PlacementManager.Singleton.QueuePlacement();
                    return;
                }
                else if (e.Buttons == GorgonLibrary.InputDevices.MouseButtons.Right)
                {
                    PlacementManager.Singleton.CancelPlacement();
                    return;
                }
                else if (e.Buttons == GorgonLibrary.InputDevices.MouseButtons.Middle)
                {
                    PlacementManager.Singleton.nextRot();
                }
            }

            if (playerController.controlledAtom == null)
                return;

            if (UiManager.Singleton.MouseDown(e))// MouseDown returns true if the click is handled by the ui component.
                return;

            #region Object clicking
            bool atomClicked = false;
            // Convert our click from screen -> world coordinates
            //Vector2D worldPosition = new Vector2D(e.Position.X + xTopLeft, e.Position.Y + yTopLeft);
            // A bounding box for our click
            System.Drawing.RectangleF mouseAABB = new System.Drawing.RectangleF(mousePosWorld.X, mousePosWorld.Y, 1, 1);
            float checkDistance = map.tileSpacing * 1.5f;
            // Find all the atoms near us we could have clicked
            IEnumerable<Atom.Atom> atoms = from a in atomManager.atomDictionary.Values
                                           where editMode ? true : (playerController.controlledAtom.position - a.position).Length < checkDistance
                                           where a.visible
                                           orderby (new Vector2D(a.sprite.AABB.X + (a.sprite.AABB.Width/2),a.sprite.AABB.Y + (a.sprite.AABB.Height/2)) - new Vector2D(mouseAABB.X, mouseAABB.Y)).Length descending
                                           orderby a.drawDepth descending
                                           select a;
            // See which one our click AABB intersected with
            foreach (Atom.Atom a in atoms)
            {
                if (a.WasClicked(mouseAABB.Location))
                {
                    if (!editMode)
                    {
                        a.HandleClick();
                    }
                    else
                    {
                        if (e.Buttons == GorgonLibrary.InputDevices.MouseButtons.Right && a != playerController.controlledAtom)
                        {
                            NetOutgoingMessage message = mStateMgr.prg.mNetworkMgr.netClient.CreateMessage();
                            message.Write((byte)NetMessage.AtomManagerMessage);
                            message.Write((byte)AtomManagerMessage.DeleteAtom);
                            message.Write(a.Uid);
                            mStateMgr.prg.mNetworkMgr.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
                        }
                    }
                    atomClicked = true; // We clicked an atom so we don't want to send a turf click message too.
                    break;
                }

            }

            if (!atomClicked)
            {
                System.Drawing.Point clickedPoint = map.GetTileArrayPositionFromWorldPosition(mousePosWorld);
                if (clickedPoint.X > 0 && clickedPoint.Y > 0)
                {
                    NetOutgoingMessage message = mStateMgr.prg.mNetworkMgr.netClient.CreateMessage();
                    message.Write((byte)NetMessage.MapMessage);
                    message.Write((byte)MapMessage.TurfClick);
                    message.Write((short)clickedPoint.X);
                    message.Write((short)clickedPoint.Y);
                    mStateMgr.prg.mNetworkMgr.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
                }
            } 
            #endregion
        }
        public override void MouseMove(MouseInputEventArgs e)
        {
            mousePosScreen = new Vector2D(e.Position.X, e.Position.Y);
            mousePosWorld = new Vector2D(e.Position.X + xTopLeft, e.Position.Y + yTopLeft);
            UiManager.Singleton.MouseMove(e);
        }
        public override void MouseWheelMove(MouseInputEventArgs e)
        { } 
        #endregion

    }

}