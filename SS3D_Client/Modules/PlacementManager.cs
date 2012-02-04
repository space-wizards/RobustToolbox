using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SS3D.Modules;
using SS3D.Modules.Network;

using SS3D_shared;
using SS3D.States;
using SS3D.HelperClasses;

using System.Reflection;

using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

using Lidgren.Network;

using System.Windows.Forms;
using SS3D_shared.HelperClasses;
using ClientResourceManager;
using ClientServices.Map;
using ClientServices.Map.Tiles;
using ClientWindow;
using System.Drawing;
using ClientInterfaces;
using ClientServices;
using CGO;

namespace SS3D.Modules
{
    class PlacementManager
    {
        private Sprite current_sprite;
        private float current_rotation;
        private EntityTemplate current_template;
        private PlacementInformation current_permission;

        private Vector2D current_loc_screen = Vector2D.Zero;
        private Vector2D current_loc_world = Vector2D.Zero;

        public Boolean is_active { private set; get; }

        public Boolean eraser { private set; get; }

        private NetworkManager network_manager;

        private Boolean validPosition = false;

        public delegate void PlacementCanceledHandler(PlacementManager mgr);
        public event PlacementCanceledHandler PlacementCanceled;

        #region Singleton
        private static PlacementManager singleton;

        private PlacementManager() { }

        public static PlacementManager Singleton
        {
            get
            {
                if (singleton == null)
                {
                    singleton = new PlacementManager();
                }
                return singleton;
            }
        }

        #endregion

        public void Initialize(NetworkManager netMgr)
        {
            network_manager = netMgr;
            eraser = false;
            Clear();
        }

        public void HandleNetMessage(NetIncomingMessage msg)
        {
            PlacementManagerMessage messageType = (PlacementManagerMessage)msg.ReadByte();

            switch (messageType)
            {
                case PlacementManagerMessage.StartPlacement:
                    HandleStartPlacement(msg);
                    break;
                case PlacementManagerMessage.CancelPlacement:
                    Clear();
                    break;
                case PlacementManagerMessage.PlacementFailed:
                    //Sad trombone here.
                    break;
            }
        }

        private void HandleStartPlacement(NetIncomingMessage msg)
        {
            current_permission = new PlacementInformation();

            current_permission.range = msg.ReadUInt16();
            current_permission.isTile = msg.ReadBoolean();
            if (current_permission.isTile) current_permission.tileType = (TileType)msg.ReadInt32();
            else current_permission.entityType = msg.ReadString();
            current_permission.placementOption = (PlacementOption)msg.ReadByte();

            BeginPlacing(current_permission);
        }

        public void Clear()
        {
            current_sprite = null;
            current_rotation = 0;
            current_template = null;
            current_permission = null;
            current_loc_screen = Vector2D.Zero;
            current_loc_world = Vector2D.Zero;
            if (PlacementCanceled != null && is_active && !eraser) PlacementCanceled(this);
            is_active = false;
            eraser = false;
        }

        public void HandlePlacement()
        {
            if (is_active && !eraser)
                RequestPlacement();
        }

        public void HandleDeletion(Entity ent)
        {
            if (is_active && eraser)
            {
                NetOutgoingMessage message = network_manager.netClient.CreateMessage();
                message.Write((byte)NetMessage.RequestEntityDeletion);
                message.Write((int)ent.Uid);
                network_manager.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
            }
        }

        public void ToggleEraser()
        {
            if (!eraser && !is_active)
            {
                is_active = true;
                eraser = true;
            }
            else Clear();
        }

        public void BeginPlacing(PlacementInformation info)
        {
            Clear();

            current_permission = info;

            if (info.isTile)
                PreparePlacement(info.tileType);
            else
                PreparePlacement(info.entityType);
        }

        private void PreparePlacement(string templateName)
        {
            EntityTemplate template = EntityManager.Singleton.TemplateDB.GetTemplate(templateName);
            if (template == null) return;

            ComponentParameter spriteParam = template.GetBaseSpriteParamaters().FirstOrDefault(); //Will break if states not ordered correctly.
            if (spriteParam == null) return;

            string spriteName = (string)spriteParam.Parameter;
            Sprite sprite = ResMgr.Singleton.GetSprite(spriteName);

            current_sprite = sprite;
            current_template = template;
            current_rotation = 0;

            is_active = true;
        }

        private void PreparePlacement(TileType tileType)
        {
            current_sprite = ResMgr.Singleton.GetSprite("tilebuildoverlay");

            is_active = true;
        }

        private void RequestPlacement()
        {
            if (current_permission == null) return;
            if (!validPosition) return;

            NetOutgoingMessage message = network_manager.netClient.CreateMessage();

            message.Write((byte)NetMessage.PlacementManagerMessage);
            message.Write((byte)PlacementManagerMessage.RequestPlacement);
            message.Write((byte)PlacementOption.AlignNone);

            message.Write(current_permission.isTile);

            if (current_permission.isTile) message.Write((int)current_permission.tileType);
            else message.Write(current_permission.entityType);

            message.Write(current_loc_world.X);
            message.Write(current_loc_world.Y);
            message.Write(current_rotation);

            network_manager.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
        }

        public void Update(Vector2D mouseScreen, Map currentMap)
        {
            if (currentMap == null) return;

            current_loc_screen = mouseScreen;

            current_loc_world = new Vector2D(mouseScreen.X + ClientWindowData.Singleton.ScreenOrigin.X, mouseScreen.Y + ClientWindowData.Singleton.ScreenOrigin.Y);

            validPosition = true;

            if (current_permission != null)
            {
                RectangleF spriteRectWorld = new RectangleF(current_loc_world.X - (current_sprite.Width / 2f), current_loc_world.Y - (current_sprite.Height / 2f), current_sprite.Width, current_sprite.Height);

                #region AlignNone
                if (current_permission.placementOption == PlacementOption.AlignNone || current_permission.placementOption == PlacementOption.AlignNoneFree)
                {
                    if (current_permission.isTile)
                    {
                        validPosition = false;
                        return;
                    }

                    CollisionManager collisionMgr = (CollisionManager)ServiceManager.Singleton.GetService(ClientServiceType.CollisionManager);
                    if (collisionMgr.IsColliding(spriteRectWorld, true)) validPosition = false;

                    if (currentMap.IsSolidTile(current_loc_world)) validPosition = false; //HANDLE CURSOR OUTSIDE MAP

                    if (current_permission.placementOption == PlacementOption.AlignNone) //AlignNoneFree does not check for range.
                        if ((PlayerController.Singleton.controlledAtom.Position - current_loc_world).Length > current_permission.range) validPosition = false;
                } 
                #endregion

                #region AlignSimilar
                else if (current_permission.placementOption == PlacementOption.AlignSimilar || current_permission.placementOption == PlacementOption.AlignSimilarFree)
                {
                    if (current_permission.isTile)
                    {
                        validPosition = false;
                        return;
                    }

                    //Align to similar if nearby found else free
                    if (currentMap.IsSolidTile(current_loc_world)) validPosition = false; //HANDLE CURSOR OUTSIDE MAP

                    if (current_permission.placementOption == PlacementOption.AlignSimilar)
                        if ((PlayerController.Singleton.controlledAtom.Position - current_loc_world).Length > current_permission.range) validPosition = false;

                    float snapToRange = 55;
                    Entity[] nearbyEntities = EntityManager.Singleton.GetEntitiesInRange(current_loc_world, snapToRange);

                    var snapToEntities = from Entity ent in nearbyEntities
                                         where ent.template == current_template
                                         orderby (ent.Position - current_loc_world).Length ascending
                                         select ent;

                    if (snapToEntities.Any())
                    {
                        Entity closestEnt = snapToEntities.First();
                        List<ComponentReplyMessage> replies = new List<ComponentReplyMessage>();
                        closestEnt.SendMessage(this, SS3D_shared.GO.ComponentMessageType.GetSprite, replies);

                        //if(replies.Any(x => x.messageType == SS3D_shared.GO.ComponentMessageType.CurrentSprite))
                        //{
                        //    Sprite closestSprite = (Sprite)replies.Find(x => x.messageType == SS3D_shared.GO.ComponentMessageType.CurrentSprite).paramsList[0]; //This is safer but slower.

                        if (replies.Any())
                        {
                            Sprite closestSprite = (Sprite)replies.First().paramsList[0]; //This is faster but kinda unsafe.

                            RectangleF closestRect = new RectangleF(closestEnt.Position.X - closestSprite.Width / 2f, closestEnt.Position.Y - closestSprite.Height / 2f, closestSprite.Width, closestSprite.Height);

                            List<Vector2D> sides = new List<Vector2D>();
                            sides.Add(new Vector2D(closestRect.X + (closestRect.Width / 2f), closestRect.Top - current_sprite.Height / 2f)); //Top
                            sides.Add(new Vector2D(closestRect.X + (closestRect.Width / 2f), closestRect.Bottom + current_sprite.Height / 2f));//Bottom
                            sides.Add(new Vector2D(closestRect.Left - current_sprite.Width / 2f, closestRect.Y + (closestRect.Height / 2f)));//Left
                            sides.Add(new Vector2D(closestRect.Right + current_sprite.Width / 2f, closestRect.Y + (closestRect.Height / 2f)));//Right

                            Vector2D closestSide = (from Vector2D side in sides orderby (side - current_loc_world).Length ascending select side).First();

                            current_loc_world = closestSide;
                            current_loc_screen = new Vector2D(closestSide.X - ClientWindowData.Singleton.ScreenOrigin.X, closestSide.Y - ClientWindowData.Singleton.ScreenOrigin.Y);
                        }
                    }

                    spriteRectWorld = new RectangleF(current_loc_world.X - (current_sprite.Width / 2f), current_loc_world.Y - (current_sprite.Height / 2f), current_sprite.Width, current_sprite.Height);
                    CollisionManager collisionMgr = (CollisionManager)ServiceManager.Singleton.GetService(ClientServiceType.CollisionManager);
                    if (collisionMgr.IsColliding(spriteRectWorld, true)) validPosition = false;
                }
                #endregion

                #region AlignTile
                else if (current_permission.placementOption == PlacementOption.AlignTileAny ||
                 current_permission.placementOption == PlacementOption.AlignTileAnyFree ||
                 current_permission.placementOption == PlacementOption.AlignTileEmpty ||
                 current_permission.placementOption == PlacementOption.AlignTileEmptyFree ||
                 current_permission.placementOption == PlacementOption.AlignTileNonSolid ||
                 current_permission.placementOption == PlacementOption.AlignTileNonSolidFree ||
                 current_permission.placementOption == PlacementOption.AlignTileSolid ||
                 current_permission.placementOption == PlacementOption.AlignTileSolidFree)
                {
                    if (current_permission.placementOption == PlacementOption.AlignTileAny ||
                        current_permission.placementOption == PlacementOption.AlignTileEmpty ||
                        current_permission.placementOption == PlacementOption.AlignTileNonSolid ||
                        current_permission.placementOption == PlacementOption.AlignTileSolid)
                        if ((PlayerController.Singleton.controlledAtom.Position - current_loc_world).Length > current_permission.range) validPosition = false;

                    if (current_permission.placementOption == PlacementOption.AlignTileNonSolid || current_permission.placementOption == PlacementOption.AlignTileNonSolidFree)
                        if (currentMap.IsSolidTile(current_loc_world)) validPosition = false;

                    if (current_permission.placementOption == PlacementOption.AlignTileSolid || current_permission.placementOption == PlacementOption.AlignTileSolidFree)
                        if (!currentMap.IsSolidTile(current_loc_world)) validPosition = false;

                    if (current_permission.placementOption == PlacementOption.AlignTileEmpty || current_permission.placementOption == PlacementOption.AlignTileEmptyFree)
                        validPosition = validPosition; //TBA.

                    if (validPosition)
                    {
                        Tile currentTile = currentMap.GetTileAt(current_loc_world);
                        if (currentTile != null)
                        {
                            if (current_permission.isTile)
                            {
                                current_loc_world = (currentTile.position + new Vector2D(currentMap.tileSpacing / 2f, currentMap.tileSpacing / 2f));
                                current_loc_screen = new Vector2D(current_loc_world.X - ClientWindowData.Singleton.ScreenOrigin.X, current_loc_world.Y - ClientWindowData.Singleton.ScreenOrigin.Y);

                                //TileRectWorld = new RectangleF(current_loc_world.X - (current_sprite.Width / 2f), current_loc_world.Y - (current_sprite.Height / 2f), current_sprite.Width, current_sprite.Height);
                                //CollisionManager collisionMgr = (CollisionManager)ServiceManager.Singleton.GetService(ClientServiceType.CollisionManager);
                                //if (collisionMgr.IsColliding(spriteRectWorld, true)) validPosition = false; //This also includes walls. Meaning that even when set to solid only this will be unplacable. Fix this.
                            }
                            else
                            {
                                current_loc_world = (currentTile.position + new Vector2D(currentMap.tileSpacing / 2f, currentMap.tileSpacing / 2f)) + new Vector2D(current_template.placementOffset.Key, current_template.placementOffset.Value);
                                current_loc_screen = new Vector2D(current_loc_world.X - ClientWindowData.Singleton.ScreenOrigin.X, current_loc_world.Y - ClientWindowData.Singleton.ScreenOrigin.Y);

                                spriteRectWorld = new RectangleF(current_loc_world.X - (current_sprite.Width / 2f), current_loc_world.Y - (current_sprite.Height / 2f), current_sprite.Width, current_sprite.Height);
                                CollisionManager collisionMgr = (CollisionManager)ServiceManager.Singleton.GetService(ClientServiceType.CollisionManager);
                                if (collisionMgr.IsColliding(spriteRectWorld, true)) validPosition = false; //This also includes walls. Meaning that even when set to solid only this will be unplacable. Fix this.
                            }
                        }
                        else validPosition = false;
                    }

                }  
                #endregion

                #region AlignWall
                else if (current_permission.placementOption == PlacementOption.AlignWall || current_permission.placementOption == PlacementOption.AlignWallFree)
                {
                    if (current_permission.isTile)
                    {
                        validPosition = false;
                        return;
                    }

                    //CollisionManager collisionMgr = (CollisionManager)ServiceManager.Singleton.GetService(ClientServiceType.CollisionManager);
                    //if (collisionMgr.IsColliding(spriteRectWorld, true)) validPosition = false;

                    if (!currentMap.IsSolidTile(current_loc_world)) validPosition = false;

                    if (current_permission.placementOption == PlacementOption.AlignWall)
                        if ((PlayerController.Singleton.controlledAtom.Position - current_loc_world).Length > current_permission.range) validPosition = false;

                    if (validPosition)
                    {
                        Tile currentTile = currentMap.GetTileAt(current_loc_world);
                        List<Vector2D> nodes = new List<Vector2D>();

                        if (current_template.mountingPoints != null)
                        {
                            foreach (int current in current_template.mountingPoints)
                                nodes.Add(new Vector2D(current_loc_world.X, currentTile.position.Y + current));
                        }
                        else
                        {
                            nodes.Add(new Vector2D(current_loc_world.X, currentTile.position.Y + 16));
                            nodes.Add(new Vector2D(current_loc_world.X, currentTile.position.Y + 32));
                            nodes.Add(new Vector2D(current_loc_world.X, currentTile.position.Y + 48));
                        }

                        Vector2D closestNode = (from Vector2D node in nodes
                                                orderby (node - current_loc_world).Length ascending
                                                select node).First();

                        current_loc_world = Vector2D.Add(closestNode, new Vector2D(current_template.placementOffset.Key, current_template.placementOffset.Value));
                        current_loc_screen = new Vector2D(current_loc_world.X - ClientWindowData.Singleton.ScreenOrigin.X, current_loc_world.Y - ClientWindowData.Singleton.ScreenOrigin.Y);

                        if (current_permission.placementOption == PlacementOption.AlignWall)
                            if ((PlayerController.Singleton.controlledAtom.Position - current_loc_world).Length > current_permission.range) validPosition = false;
                    }
                } 
                #endregion

                else if (current_permission.placementOption == PlacementOption.Freeform)
                {
                    validPosition = true; //Herpderp
                }
            }
        }

        public void Render()
        {
            if (current_sprite != null)
            {
                current_sprite.Color = validPosition ? Color.ForestGreen : Color.IndianRed;
                current_sprite.Position = new Vector2D(current_loc_screen.X - (current_sprite.Width / 2f), current_loc_screen.Y - (current_sprite.Height / 2f)); //Centering the sprite on the cursor.
                current_sprite.Draw();
                current_sprite.Color = Color.White;
            }

            if (current_permission != null)
            {
                if (current_permission.placementOption == PlacementOption.AlignNone    ||
                    current_permission.placementOption == PlacementOption.AlignSimilar ||
                    current_permission.placementOption == PlacementOption.AlignTileAny ||
                    current_permission.placementOption == PlacementOption.AlignTileEmpty ||
                    current_permission.placementOption == PlacementOption.AlignTileNonSolid ||
                    current_permission.placementOption == PlacementOption.AlignTileSolid ||
                    current_permission.placementOption == PlacementOption.AlignWall)   //If it uses range, show the range.
                {
                    Gorgon.Screen.Circle(
                        PlayerController.Singleton.controlledAtom.Position.X - ClientWindowData.Singleton.ScreenOrigin.X,
                        PlayerController.Singleton.controlledAtom.Position.Y - ClientWindowData.Singleton.ScreenOrigin.Y,
                        current_permission.range,
                        Color.DeepSkyBlue,
                        new Vector2D(2, 2));
                }
            }
        }
            
    }
}
