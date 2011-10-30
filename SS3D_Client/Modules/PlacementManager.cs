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

        public void BeginErasing()
        {
            Clear();
            is_active = true;
            eraser = true;
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

            current_loc_world = new Vector2D(mouseScreen.X + ClientWindowData.xTopLeft, mouseScreen.Y + ClientWindowData.yTopLeft);

            validPosition = true;

            if (current_permission != null)
            {
                RectangleF spriteRectWorld = new RectangleF(current_loc_world.X - (current_sprite.Width / 2f), current_loc_world.Y - (current_sprite.Height / 2f), current_sprite.Width, current_sprite.Height);

                if (current_permission.placementOption == PlacementOption.AlignNone || current_permission.placementOption == PlacementOption.AlignNoneFree)
                {
                    CollisionManager collisionMgr = (CollisionManager)ServiceManager.Singleton.GetService(ClientServiceType.CollisionManager);
                    if (collisionMgr.IsColliding(spriteRectWorld, true)) validPosition = false;

                    if (currentMap.IsSolidTile(current_loc_world)) validPosition = false; //HANDLE CURSOR OUTSIDE MAP

                    if (current_permission.placementOption == PlacementOption.AlignNone) //AlignNoneFree does not check for range.
                        if ((PlayerController.Singleton.controlledAtom.Position - current_loc_world).Length > current_permission.range) validPosition = false;
                }

                else if (current_permission.placementOption == PlacementOption.AlignSimilar || current_permission.placementOption == PlacementOption.AlignSimilarFree)
                {
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
                            current_loc_screen = new Vector2D(closestSide.X - ClientWindowData.xTopLeft, closestSide.Y - ClientWindowData.yTopLeft);
                        }
                    }

                    spriteRectWorld = new RectangleF(current_loc_world.X - (current_sprite.Width / 2f), current_loc_world.Y - (current_sprite.Height / 2f), current_sprite.Width, current_sprite.Height);
                    CollisionManager collisionMgr = (CollisionManager)ServiceManager.Singleton.GetService(ClientServiceType.CollisionManager);
                    if (collisionMgr.IsColliding(spriteRectWorld, true)) validPosition = false;
                }

                else if (current_permission.placementOption == PlacementOption.AlignTileAny || //DONT LOOK AT ME. IM HIDEOUS. Gonna replace this with flags later.
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
                            current_loc_world = (currentTile.position + new Vector2D(currentMap.tileSpacing / 2f, currentMap.tileSpacing / 2f)) + new Vector2D(current_template.placementOffset.Key, current_template.placementOffset.Value);
                            current_loc_screen = new Vector2D(current_loc_world.X - ClientWindowData.xTopLeft, current_loc_world.Y - ClientWindowData.yTopLeft);

                            spriteRectWorld = new RectangleF(current_loc_world.X - (current_sprite.Width / 2f), current_loc_world.Y - (current_sprite.Height / 2f), current_sprite.Width, current_sprite.Height);
                            CollisionManager collisionMgr = (CollisionManager)ServiceManager.Singleton.GetService(ClientServiceType.CollisionManager);
                            if (collisionMgr.IsColliding(spriteRectWorld, true)) validPosition = false; //This also includes walls. Meaning that even when set to solid only this will be unplacable. Fix this.
                        }
                        else validPosition = false;
                    }

                }

                else if (current_permission.placementOption == PlacementOption.AlignWall || current_permission.placementOption == PlacementOption.AlignWallFree)
                {
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

                        current_loc_world = Vector2D.Add(closestNode , new Vector2D(current_template.placementOffset.Key, current_template.placementOffset.Value));
                        current_loc_screen = new Vector2D(current_loc_world.X - ClientWindowData.xTopLeft, current_loc_world.Y - ClientWindowData.yTopLeft);

                        if (current_permission.placementOption == PlacementOption.AlignWall)
                            if ((PlayerController.Singleton.controlledAtom.Position - current_loc_world).Length > current_permission.range) validPosition = false;
                    }
                }

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
                    Gorgon.Screen.Circle(PlayerController.Singleton.controlledAtom.Position.X - ClientWindowData.xTopLeft, PlayerController.Singleton.controlledAtom.Position.Y - ClientWindowData.yTopLeft, current_permission.range, Color.DeepSkyBlue, new Vector2D(2, 2));
                }
            }
        }
            
    }

    //class PlacementManager
    //{
    //    private Map map;
    //    private AtomManager atomManager;
    //    private GameScreen gameScreen;
    //    private NetworkManager networkMgr;

    //    private float rotation = 0f;

    //    public BuildPermission active { private set; get; }

    //    Sprite previewSprite;
    //    Type activeType;
    //    Atom.Atom snapToAtom; //The current atom for snap-to-similar
    //    Boolean validLocation = false;
    //    Boolean previewVisible = true;

    //    Boolean placementQueued = false;

    //    Vector2D snapToLoc = Vector2D.Zero;

    //    #region Singleton
    //    private static PlacementManager singleton;

    //    private PlacementManager() { }

    //    public static PlacementManager Singleton
    //    {
    //        get
    //        {
    //            if (singleton == null)
    //            {
    //                singleton = new PlacementManager();
    //            }
    //            return singleton;
    //        }
    //    }

    //    #endregion

    //    public void Initialize(Map _map, AtomManager _atom, GameScreen _screen, NetworkManager netMgr)
    //    {
    //        map = _map;
    //        atomManager = _atom;
    //        gameScreen = _screen;
    //        networkMgr = netMgr;
    //    }

    //    public void Reset()
    //    {
    //        map = null;
    //        atomManager = null;
    //        gameScreen = null;
    //        networkMgr = null;
    //        active = null;
    //        snapToAtom = null;
    //        previewSprite = null;
    //    }

    //    public void nextRot()
    //    {
    //        rotation = (rotation + 90f) == 360 ? 0 : rotation + 90;          
    //    }

    //    public void HandleNetMessage(NetIncomingMessage msg)
    //    {
    //        PlacementManagerMessage messageType = (PlacementManagerMessage)msg.ReadByte();

    //        switch (messageType)
    //        {
    //            case PlacementManagerMessage.StartPlacement:
    //                HandleStartPlacement(msg);
    //                break;
    //            case PlacementManagerMessage.CancelPlacement:
    //                CancelPlacement();
    //                break;
    //            case PlacementManagerMessage.PlacementFailed:
    //                break;
    //        }
    //    }

    //    public void CancelPlacement()
    //    {
    //        previewSprite = null;
    //        activeType = null;
    //        snapToAtom = null;
    //        validLocation = false;
    //        previewVisible = true;
    //        placementQueued = false;
    //        snapToLoc = Vector2D.Zero;
    //        active = null;
    //    }

    //    public void SendObjectRequestEDITMODE(Type type, AlignmentOptions alignMode)
    //    {
    //        string typeStr = type.ToString();
    //        typeStr = typeStr.Substring(typeStr.IndexOf(".") + 1); // Fuckugly method of stripping the assembly name of the type.

    //        NetOutgoingMessage message = networkMgr.netClient.CreateMessage();
    //        message.Write((byte)NetMessage.PlacementManagerMessage);
    //        message.Write((byte)PlacementManagerMessage.EDITMODE_GetObject);
    //        message.Write(typeStr);
    //        message.Write((byte)alignMode);
    //        networkMgr.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
    //    }

    //    private void HandleStartPlacement(NetIncomingMessage msg)
    //    {
    //        active = new BuildPermission();
    //        active.range = msg.ReadUInt16();
    //        active.type = msg.ReadString();
    //        active.AlignOption = (AlignmentOptions)msg.ReadByte();
    //        active.placeAnywhere = msg.ReadBoolean();

    //        SetupPlacement();
    //    }

    //    private Boolean isSolidTile(Tile tile)
    //    {
    //        if (tile.tileType != TileType.Wall) return false;
    //        else return true;
    //    }

    //    private Boolean isSolidTile(TileType tiletype)
    //    {
    //        if (tiletype != TileType.Wall) return false;
    //        else return true;
    //    }

    //    private void SetupPlacement()
    //    {
    //        Type atomType = atomManager.GetAtomType(active.type);
    //        activeType = atomType;
    //        previewSprite = ResMgr.Singleton.GetSprite(Utilities.GetObjectSpriteName(atomType));
    //        placementQueued = false;
    //    }

    //    public void QueuePlacement() 
    //    {   
    //        //Clicking wont instantly place the object.
    //        //Instead the manager will try to place it next update.
    //        //This is a bit ugly but it'll work for now.
    //        //With any luck this wont be noticeable at all.
    //        if (placementQueued) return;
    //        placementQueued = true;
    //    }

    //    private void RequestPlacement(Vector2D pos)
    //    {
    //        NetOutgoingMessage message = networkMgr.netClient.CreateMessage();
    //        message.Write((byte)NetMessage.PlacementManagerMessage);
    //        message.Write((byte)PlacementManagerMessage.RequestPlacement);
    //        message.Write((byte)active.AlignOption);
    //        message.Write(pos.X);
    //        message.Write(pos.Y);
    //        message.Write(rotation);
    //        networkMgr.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
    //        placementQueued = false;
    //    }

    //    public void Update()
    //    {
    //        if (active != null)
    //        {
    //            validLocation = true;
    //            previewVisible = true;

    //            switch (active.AlignOption)
    //            {
    //                #region Align None
    //                case AlignmentOptions.AlignNone:
    //                    System.Drawing.Point arrayPos = map.GetTileArrayPositionFromWorldPosition(gameScreen.mousePosWorld);
    //                    TileType type = map.GetTileTypeFromArrayPosition(arrayPos.X, arrayPos.Y);

    //                    if (isSolidTile(type)) validLocation = false;

    //                    var atomsBlocking = from a in atomManager.atomDictionary.Values
    //                                        where a.collidable
    //                                        where a.GetAABB().IntersectsWith(previewSprite.AABB) || a.GetAABB().IntersectsWith(new System.Drawing.RectangleF(gameScreen.mousePosWorld.X,gameScreen.mousePosWorld.Y,2,2))
    //                                        select a;

    //                    if (atomsBlocking.Any() || (gameScreen.mousePosWorld - gameScreen.playerController.controlledAtom.Position).Length > active.range)
    //                        validLocation = false;

    //                    if (active.placeAnywhere) validLocation = true;

    //                    if (validLocation && placementQueued)
    //                        RequestPlacement(gameScreen.mousePosWorld);
    //                    else if(!validLocation && placementQueued)
    //                        placementQueued = false;

    //                    break; 
    //                #endregion

    //                #region Align Similar
    //                case AlignmentOptions.AlignSimilar:
    //                    var atoms = from a in atomManager.atomDictionary.Values
    //                                where a.IsTypeOf(activeType)
    //                                where a.visible
    //                                where active.placeAnywhere ? true : (a.Position - gameScreen.mousePosWorld).Length <= active.range * 2
    //                                where active.placeAnywhere ? true : (a.Position - gameScreen.playerController.controlledAtom.Position).Length <= active.range
    //                                orderby (a.Position - gameScreen.mousePosWorld).Length ascending
    //                                select a; //Basically: Get the closest similar object.

    //                    if (atoms.Count() > 0)
    //                    {
    //                        //This assumes that the last frames AABBs are useable and sorta accurate.
    //                        Vector2D topConnection = new Vector2D(atoms.First().GetAABB().Location.X + atoms.First().GetAABB().Width / 2, atoms.First().GetAABB().Top - atoms.First().GetAABB().Height / 2);
    //                        Vector2D bottomConnection = new Vector2D(atoms.First().GetAABB().Location.X + atoms.First().GetAABB().Width / 2, atoms.First().GetAABB().Bottom + atoms.First().GetAABB().Height / 2);
    //                        Vector2D leftConnection = new Vector2D(atoms.First().GetAABB().Left - atoms.First().GetAABB().Width / 2, atoms.First().GetAABB().Location.Y + atoms.First().GetAABB().Height / 2);
    //                        Vector2D rightConnection = new Vector2D(atoms.First().GetAABB().Right + atoms.First().GetAABB().Width / 2, atoms.First().GetAABB().Location.Y + atoms.First().GetAABB().Height / 2);

    //                        List<Vector2D> sideConnections = new List<Vector2D>();
    //                        sideConnections.Add(topConnection);
    //                        sideConnections.Add(bottomConnection);
    //                        sideConnections.Add(leftConnection);
    //                        sideConnections.Add(rightConnection);

    //                        var closestSide = from Vector2D vec in sideConnections
    //                                          where active.placeAnywhere ? true : (vec - gameScreen.playerController.controlledAtom.Position).Length <= active.range
    //                                          where active.placeAnywhere ? true : !isSolidTile(map.GetTileAt(vec))
    //                                          orderby (vec - gameScreen.mousePosWorld).Length ascending
    //                                          select vec;

    //                        if (closestSide.Any())
    //                        {
    //                            snapToLoc = new Vector2D(closestSide.First().X - ClientWindowData.xTopLeft, closestSide.First().Y - ClientWindowData.yTopLeft);
    //                            if (validLocation && placementQueued)
    //                                RequestPlacement(closestSide.First());
    //                        }
    //                        else //No side in range. This shouldnt be possible if the object itself is in range.
    //                        {
    //                            validLocation = false;
    //                            previewVisible = false;
    //                            placementQueued = false;
    //                        }
    //                        snapToAtom = atoms.First();
    //                    }
    //                    else //Nothing in range.
    //                    {
    //                        validLocation = false;
    //                        previewVisible = false;
    //                        placementQueued = false;
    //                    }
    //                    break; 
    //                #endregion

    //                #region Align Wall
    //                case AlignmentOptions.AlignWall:
    //                    Tile wall = map.GetTileAt(gameScreen.mousePosWorld);

    //                    //switch ((int)rotation) //East and west are switched around because objects "attach" to the walls.
    //                    //{
    //                    //    case 0:   // North = 1
    //                    //        if ((wall.surroundDirs & Constants.NORTH) == Constants.NORTH) validLocation = false;
    //                    //        break;
    //                    //    case 270:  // East = 2
    //                    //        if ((wall.surroundDirs & Constants.EAST) == Constants.EAST) validLocation = false;
    //                    //        break;
    //                    //    case 180: // South = 4
    //                    //        if ((wall.surroundDirs & Constants.SOUTH) == Constants.SOUTH) validLocation = false;
    //                    //        break;
    //                    //    case 90: // West = 8
    //                    //        if ((wall.surroundDirs & Constants.WEST) == Constants.WEST) validLocation = false;
    //                    //        break;
    //                    //}

    //                    if (isSolidTile(wall))
    //                    {
    //                        Vector2D Node1 = new Vector2D(gameScreen.mousePosWorld.X, wall.position.Y + 1);//Bit ugly.
    //                        Vector2D Node2 = new Vector2D(gameScreen.mousePosWorld.X, wall.position.Y + 9);
    //                        Vector2D Node3 = new Vector2D(gameScreen.mousePosWorld.X, wall.position.Y + 17);
    //                        Vector2D Node4 = new Vector2D(gameScreen.mousePosWorld.X, wall.position.Y + 25);
    //                        Vector2D Node5 = new Vector2D(gameScreen.mousePosWorld.X, wall.position.Y + 33);
    //                        Vector2D Node6 = new Vector2D(gameScreen.mousePosWorld.X, wall.position.Y + 41);
    //                        Vector2D Node7 = new Vector2D(gameScreen.mousePosWorld.X, wall.position.Y + 49);
    //                        Vector2D Node8 = new Vector2D(gameScreen.mousePosWorld.X, wall.position.Y + 57);

    //                        List<Vector2D> Nodes = new List<Vector2D>();
    //                        Nodes.Add(Node1);
    //                        Nodes.Add(Node2);
    //                        Nodes.Add(Node3);
    //                        Nodes.Add(Node4);
    //                        Nodes.Add(Node5);
    //                        Nodes.Add(Node6);
    //                        Nodes.Add(Node7);
    //                        Nodes.Add(Node8);

    //                        var closestNode = from Vector2D vec in Nodes
    //                                          where active.placeAnywhere ? true : (vec - gameScreen.playerController.controlledAtom.Position).Length <= active.range
    //                                          orderby (vec - gameScreen.mousePosWorld).Length ascending
    //                                          select vec;

    //                        if (closestNode.Any())
    //                        {
    //                            snapToLoc = new Vector2D(closestNode.First().X - ClientWindowData.xTopLeft, closestNode.First().Y - ClientWindowData.yTopLeft);
    //                            if (validLocation && placementQueued)
    //                                RequestPlacement(closestNode.First());
    //                            else if (!validLocation && placementQueued)
    //                                placementQueued = false;
    //                        }
    //                        else //No node in range. This shouldnt be possible if the object itself is in range.
    //                        {
    //                            validLocation = false;
    //                            previewVisible = false;
    //                            placementQueued = false;
    //                        }
    //                    }
    //                    else //Not a supported tile. Or not in range.
    //                    {
    //                        validLocation = false;
    //                        previewVisible = false;
    //                        placementQueued = false;
    //                    }
    //                    break;
    //                #endregion

    //                #region Align Tile
    //                case AlignmentOptions.AlignTile:
    //                    ClientServices.Map.Tiles.Tile tile = map.GetTileAt(gameScreen.mousePosWorld);
    //                    snapToLoc = new Vector2D(tile.position.X + (map.tileSpacing / 2) - ClientWindowData.xTopLeft, tile.position.Y + (map.tileSpacing / 2) - ClientWindowData.yTopLeft);
    //                    if ((new Vector2D(tile.position.X + (map.tileSpacing / 2), tile.position.Y + (map.tileSpacing / 2)) - gameScreen.playerController.controlledAtom.Position).Length > active.range && !active.placeAnywhere) validLocation = false;

    //                    if (activeType.IsSubclassOf(typeof(ClientServices.Map.Tiles.Tile)))
    //                    {//Special handling for tiles? Not right now.
    //                    }
    //                    else if(activeType.IsSubclassOf(typeof(Atom.Atom)))
    //                    {
    //                        if (isSolidTile(tile) && !active.placeAnywhere) validLocation = false;
    //                    }

    //                    if (validLocation && placementQueued)
    //                        RequestPlacement(new Vector2D(tile.position.X + (map.tileSpacing / 2), tile.position.Y + (map.tileSpacing / 2)));
    //                    else if (!validLocation && placementQueued)
    //                        placementQueued = false;

    //                    break; 
    //                #endregion
    //            }
    //        }
    //    }

    //    public void Draw()
    //    {
    //        if (active != null)
    //        {
    //            Vector2D adjusted = Vector2D.Zero;

    //            switch (active.AlignOption)
    //            {
    //                case AlignmentOptions.AlignSimilar:
    //                    adjusted = snapToLoc;
    //                    break;
    //                case AlignmentOptions.AlignTile:
    //                    if (previewSprite.Axis == Vector2D.Zero) //Not all sprites are centered.
    //                        adjusted = new Vector2D(snapToLoc.X - (previewSprite.Width / 2), snapToLoc.Y - (previewSprite.Height / 2)); //Not centered. Draw it centered.
    //                    else
    //                        adjusted = snapToLoc; //Centered. Draw it where it is.
    //                    break;
    //                case AlignmentOptions.AlignWall:
    //                    adjusted = snapToLoc;
    //                    break;
    //                case AlignmentOptions.AlignNone:
    //                    adjusted = gameScreen.mousePosScreen;
    //                    break;
    //            }

    //            if (previewVisible)
    //            {
    //                previewSprite.Position = adjusted;
    //                previewSprite.Color = validLocation ? System.Drawing.Color.LimeGreen : System.Drawing.Color.Red;
    //                if (!activeType.IsSubclassOf(typeof(ClientServices.Map.Tiles.Tile))) previewSprite.Rotation = rotation;
    //                previewSprite.Opacity = 90;
    //                previewSprite.Draw();
    //                previewSprite.Color = System.Drawing.Color.White;
    //            }

    //            if (gameScreen.playerController.controlledAtom != null && !active.placeAnywhere)
    //            {
    //                Gorgon.Screen.Circle(gameScreen.playerController.controlledAtom.Position.X - ClientWindowData.xTopLeft, gameScreen.playerController.controlledAtom.Position.Y - ClientWindowData.yTopLeft, active.range, System.Drawing.Color.DarkBlue, 2f, 2f);
    //            }

    //            #region Debug Display
    //            if (previewVisible && validLocation)
    //            {
    //                switch (active.AlignOption)
    //                {
    //                    case AlignmentOptions.AlignSimilar:
    //                        if (snapToAtom != null)
    //                        {
    //                            Gorgon.Screen.Line(snapToAtom.Position.X - ClientWindowData.xTopLeft, snapToAtom.Position.Y - ClientWindowData.yTopLeft, -((snapToAtom.Position.X - ClientWindowData.xTopLeft) - snapToLoc.X), -((snapToAtom.Position.Y - ClientWindowData.yTopLeft) - snapToLoc.Y), System.Drawing.Color.White, new Vector2D(3, 3));
    //                            Gorgon.Screen.FilledCircle(snapToLoc.X, snapToLoc.Y, 3, System.Drawing.Color.LimeGreen);
    //                            Gorgon.Screen.FilledCircle(snapToAtom.Position.X - ClientWindowData.xTopLeft, snapToAtom.Position.Y - ClientWindowData.yTopLeft, 3, System.Drawing.Color.LimeGreen);
    //                        }
    //                        break;
    //                    case AlignmentOptions.AlignTile:
    //                        break;
    //                    case AlignmentOptions.AlignWall:
    //                        break;
    //                    case AlignmentOptions.AlignNone:
    //                        Gorgon.Screen.FilledCircle(snapToLoc.X, snapToLoc.Y, 3, System.Drawing.Color.LimeGreen);
    //                        break;
    //                }
    //            }
    //            #endregion
    //        }
    //    }
    //}
}
