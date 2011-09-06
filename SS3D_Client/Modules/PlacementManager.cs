using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SS3D.Modules;
using SS3D.Modules.Map;
using SS3D.Modules.Network;
using SS3D.Modules.UI;
using SS3D.Atom;

using SS3D_shared;
using SS3D.States;

using System.Reflection;

using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

using Lidgren.Network;

using System.Windows.Forms;
using SS3D_shared.HelperClasses;
using SS3D_shared;

namespace SS3D.Modules
{
    class PlacementManager
    {
        private Map.Map map;
        private AtomManager atomManager;
        private GameScreen gameScreen;
        private NetworkManager networkMgr;

        private float rotation = 0f;

        public BuildPermission active { private set; get; }

        Sprite previewSprite;
        Type activeType;
        Atom.Atom snapToAtom; //The current atom for snap-to-similar
        byte snapToSide;      //The current side of the current atom for snap-to-similar. 1 Top, 2 Right, 3 Bottom, 4 Left. Unused.
        Boolean validLocation = false;
        Boolean previewVisible = true;

        Boolean placementQueued = false;

        Vector2D snapToLoc = Vector2D.Zero;

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

        public void Initialize(Map.Map _map, AtomManager _atom, GameScreen _screen, NetworkManager netMgr)
        {
            map = _map;
            atomManager = _atom;
            gameScreen = _screen;
            networkMgr = netMgr;
        }

        public void nextRot()
        {
            rotation = (rotation + 90f) == 360 ? 0 : rotation + 90;          
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
                    CancelPlacement();
                    break;
                case PlacementManagerMessage.PlacementFailed:
                    break;
            }
        }

        public void CancelPlacement()
        {
            previewSprite = null;
            activeType = null;
            snapToAtom = null;
            snapToSide = 0;
            validLocation = false;
            previewVisible = true;
            placementQueued = false;
            snapToLoc = Vector2D.Zero;
            active = null;
        }

        public void SendObjectRequestEDITMODE(Type type, AlignmentOptions alignMode)
        {
            string typeStr = type.ToString();
            typeStr = typeStr.Substring(typeStr.IndexOf(".") + 1); // Fuckugly method of stripping the assembly name of the type.

            NetOutgoingMessage message = networkMgr.netClient.CreateMessage();
            message.Write((byte)NetMessage.PlacementManagerMessage);
            message.Write((byte)PlacementManagerMessage.EDITMODE_GetObject);
            message.Write(typeStr);
            message.Write((byte)alignMode);
            networkMgr.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
        }

        private void HandleStartPlacement(NetIncomingMessage msg)
        {
            active = new BuildPermission();
            active.range = msg.ReadUInt16();
            active.type = msg.ReadString();
            active.AlignOption = (AlignmentOptions)msg.ReadByte();
            active.placeAnywhere = msg.ReadBoolean();

            SetupPlacement();
        }

        private Boolean isSolidTile(Tiles.Tile tile)
        {
            if (tile.tileType != TileType.Wall) return false;
            else return true;
        }

        private Boolean isSolidTile(TileType tiletype)
        {
            if (tiletype != TileType.Wall) return false;
            else return true;
        }

        private void SetupPlacement()
        {
            Assembly currentAssembly = Assembly.GetExecutingAssembly();
            Type atomType = currentAssembly.GetType("SS3D." + active.type);
            activeType = atomType;
            previewSprite = ResMgr.Singleton.GetSprite(GetSpriteName(atomType));
            placementQueued = false;
        }

        public void QueuePlacement() 
        {   
            //Clicking wont instantly place the object.
            //Instead the manager will try to place it next update.
            //This is a bit ugly but it'll work for now.
            //With any luck this wont be noticeable at all.
            if (placementQueued) return;
            placementQueued = true;
        }

        private void RequestPlacement(Vector2D pos)
        {
            NetOutgoingMessage message = networkMgr.netClient.CreateMessage();
            message.Write((byte)NetMessage.PlacementManagerMessage);
            message.Write((byte)PlacementManagerMessage.RequestPlacement);
            message.Write((byte)active.AlignOption);
            message.Write(pos.X);
            message.Write(pos.Y);
            message.Write(rotation);
            networkMgr.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
            placementQueued = false;
        }

        public string GetSpriteName(Type type)
        {
            if (type.IsSubclassOf(typeof(Tiles.Tile))) //Tiles need special treatment.
            {
                return "tilebuildoverlay";
            }
            else if (type.IsSubclassOf(typeof(Atom.Atom)))
            {
                Atom.Atom atom = (Atom.Atom)Activator.CreateInstance(type);
                string strName = atom.spritename;
                atom = null;
                return strName;
            }
            return "noSprite";
        }

        public void Update()
        {
            if (active != null)
            {
                validLocation = true;
                previewVisible = true;

                switch (active.AlignOption)
                {
                    #region Align None
                    case AlignmentOptions.AlignNone:
                        System.Drawing.Point arrayPos = map.GetTileArrayPositionFromWorldPosition(gameScreen.mousePosWorld);
                        TileType type = map.GetTileTypeFromArrayPosition(arrayPos.X, arrayPos.Y);

                        if (isSolidTile(type)) validLocation = false;

                        var atomsBlocking = from a in atomManager.atomDictionary.Values
                                            where a.collidable
                                            where a.GetAABB().IntersectsWith(previewSprite.AABB) || a.GetAABB().IntersectsWith(new System.Drawing.RectangleF(gameScreen.mousePosWorld.X,gameScreen.mousePosWorld.Y,2,2))
                                            select a;

                        if (atomsBlocking.Any() || (gameScreen.mousePosWorld - gameScreen.playerController.controlledAtom.position).Length > active.range)
                            validLocation = false;

                        if (active.placeAnywhere) validLocation = true;

                        if (validLocation && placementQueued)
                            RequestPlacement(gameScreen.mousePosWorld);
                        else if(!validLocation && placementQueued)
                            placementQueued = false;

                        break; 
                    #endregion

                    #region Align Similar
                    case AlignmentOptions.AlignSimilar:
                        var atoms = from a in atomManager.atomDictionary.Values
                                    where a.IsTypeOf(activeType)
                                    where a.visible
                                    where active.placeAnywhere ? true : (a.position - gameScreen.mousePosWorld).Length <= active.range * 2
                                    where active.placeAnywhere ? true : (a.position - gameScreen.playerController.controlledAtom.position).Length <= active.range
                                    orderby (a.position - gameScreen.mousePosWorld).Length ascending
                                    select a; //Basically: Get the closest similar object.

                        if (atoms.Count() > 0)
                        {
                            //This assumes that the last frames AABBs are useable and sorta accurate.
                            Vector2D topConnection = new Vector2D(atoms.First().GetAABB().Location.X + atoms.First().GetAABB().Width / 2, atoms.First().GetAABB().Top - atoms.First().GetAABB().Height / 2);
                            Vector2D bottomConnection = new Vector2D(atoms.First().GetAABB().Location.X + atoms.First().GetAABB().Width / 2, atoms.First().GetAABB().Bottom + atoms.First().GetAABB().Height / 2);
                            Vector2D leftConnection = new Vector2D(atoms.First().GetAABB().Left - atoms.First().GetAABB().Width / 2, atoms.First().GetAABB().Location.Y + atoms.First().GetAABB().Height / 2);
                            Vector2D rightConnection = new Vector2D(atoms.First().GetAABB().Right + atoms.First().GetAABB().Width / 2, atoms.First().GetAABB().Location.Y + atoms.First().GetAABB().Height / 2);

                            List<Vector2D> sideConnections = new List<Vector2D>();
                            sideConnections.Add(topConnection);
                            sideConnections.Add(bottomConnection);
                            sideConnections.Add(leftConnection);
                            sideConnections.Add(rightConnection);

                            var closestSide = from Vector2D vec in sideConnections
                                              where active.placeAnywhere ? true : (vec - gameScreen.playerController.controlledAtom.position).Length <= active.range
                                              where active.placeAnywhere ? true : !isSolidTile(map.GetTileAt(vec))
                                              orderby (vec - gameScreen.mousePosWorld).Length ascending
                                              select vec;

                            if (closestSide.Any())
                            {
                                snapToLoc = new Vector2D(closestSide.First().X - gameScreen.xTopLeft, closestSide.First().Y - gameScreen.yTopLeft);
                                if (validLocation && placementQueued)
                                    RequestPlacement(closestSide.First());
                            }
                            else //No side in range. This shouldnt be possible if the object itself is in range.
                            {
                                validLocation = false;
                                previewVisible = false;
                                placementQueued = false;
                            }
                            snapToAtom = atoms.First();
                        }
                        else //Nothing in range.
                        {
                            validLocation = false;
                            previewVisible = false;
                            placementQueued = false;
                        }
                        break; 
                    #endregion

                    #region Align Wall
                    case AlignmentOptions.AlignWall:
                        Tiles.Tile wall = map.GetTileAt(gameScreen.mousePosWorld);

                        switch ((int)rotation) //East and west are switched around because objects "attach" to the walls.
                        {
                            case 0:   // North = 1
                                if ((wall.surroundDirs & Constants.NORTH) == Constants.NORTH) validLocation = false;
                                break;
                            case 270:  // East = 2
                                if ((wall.surroundDirs & Constants.EAST) == Constants.EAST) validLocation = false;
                                break;
                            case 180: // South = 4
                                validLocation = false; //Disabled.
                                if ((wall.surroundDirs & Constants.SOUTH) == Constants.SOUTH) validLocation = false;
                                break;
                            case 90: // West = 8
                                if ((wall.surroundDirs & Constants.WEST) == Constants.WEST) validLocation = false;
                                break;
                        }

                        if (isSolidTile(wall))
                        {
                            Vector2D Node1 = new Vector2D(gameScreen.mousePosWorld.X, wall.position.Y + 1);//Bit ugly.
                            Vector2D Node2 = new Vector2D(gameScreen.mousePosWorld.X, wall.position.Y + 9);
                            Vector2D Node3 = new Vector2D(gameScreen.mousePosWorld.X, wall.position.Y + 17);
                            Vector2D Node4 = new Vector2D(gameScreen.mousePosWorld.X, wall.position.Y + 25);
                            Vector2D Node5 = new Vector2D(gameScreen.mousePosWorld.X, wall.position.Y + 33);
                            Vector2D Node6 = new Vector2D(gameScreen.mousePosWorld.X, wall.position.Y + 41);
                            Vector2D Node7 = new Vector2D(gameScreen.mousePosWorld.X, wall.position.Y + 49);
                            Vector2D Node8 = new Vector2D(gameScreen.mousePosWorld.X, wall.position.Y + 57);

                            List<Vector2D> Nodes = new List<Vector2D>();
                            Nodes.Add(Node1);
                            Nodes.Add(Node2);
                            Nodes.Add(Node3);
                            Nodes.Add(Node4);
                            Nodes.Add(Node5);
                            Nodes.Add(Node6);
                            Nodes.Add(Node7);
                            Nodes.Add(Node8);

                            var closestNode = from Vector2D vec in Nodes
                                              where active.placeAnywhere ? true : (vec - gameScreen.playerController.controlledAtom.position).Length <= active.range
                                              orderby (vec - gameScreen.mousePosWorld).Length ascending
                                              select vec;

                            if (closestNode.Any())
                            {
                                snapToLoc = new Vector2D(closestNode.First().X - gameScreen.xTopLeft, closestNode.First().Y - gameScreen.yTopLeft);
                                if (validLocation && placementQueued)
                                    RequestPlacement(closestNode.First());
                                else if (!validLocation && placementQueued)
                                    placementQueued = false;
                            }
                            else //No node in range. This shouldnt be possible if the object itself is in range.
                            {
                                validLocation = false;
                                previewVisible = false;
                                placementQueued = false;
                            }
                        }
                        else //Not a supported tile. Or not in range.
                        {
                            validLocation = false;
                            previewVisible = false;
                            placementQueued = false;
                        }
                        break;
                    #endregion

                    #region Align Tile
                    case AlignmentOptions.AlignTile:
                        Tiles.Tile tile = map.GetTileAt(gameScreen.mousePosWorld);
                        snapToLoc = new Vector2D(tile.position.X + (map.tileSpacing / 2) - gameScreen.xTopLeft, tile.position.Y + (map.tileSpacing / 2) - gameScreen.yTopLeft);
                        if ((new Vector2D(tile.position.X + (map.tileSpacing / 2), tile.position.Y + (map.tileSpacing / 2)) - gameScreen.playerController.controlledAtom.position).Length > active.range && !active.placeAnywhere) validLocation = false;

                        if(activeType.IsSubclassOf(typeof(Tiles.Tile)))
                        {//Special handling for tiles? Not right now.
                        }
                        else if(activeType.IsSubclassOf(typeof(Atom.Atom)))
                        {
                            if (isSolidTile(tile) && !active.placeAnywhere) validLocation = false;
                        }

                        if (validLocation && placementQueued)
                            RequestPlacement(new Vector2D(tile.position.X + (map.tileSpacing / 2), tile.position.Y + (map.tileSpacing / 2)));
                        else if (!validLocation && placementQueued)
                            placementQueued = false;

                        break; 
                    #endregion
                }
            }
        }

        public void Draw()
        {
            if (active != null)
            {
                Vector2D adjusted = Vector2D.Zero;

                switch (active.AlignOption)
                {
                    case AlignmentOptions.AlignSimilar:
                        adjusted = snapToLoc;
                        break;
                    case AlignmentOptions.AlignTile:
                        if (previewSprite.Axis == Vector2D.Zero) //Not all sprites are centered.
                            adjusted = new Vector2D(snapToLoc.X - (previewSprite.Width / 2), snapToLoc.Y - (previewSprite.Height / 2)); //Not centered. Draw it centered.
                        else
                            adjusted = snapToLoc; //Centered. Draw it where it is.
                        break;
                    case AlignmentOptions.AlignWall:
                        adjusted = snapToLoc;
                        break;
                    case AlignmentOptions.AlignNone:
                        adjusted = gameScreen.mousePosScreen;
                        break;
                }

                if (previewVisible)
                {
                    previewSprite.Position = adjusted;
                    previewSprite.Color = validLocation ? System.Drawing.Color.LimeGreen : System.Drawing.Color.Red;
                    if (!activeType.IsSubclassOf(typeof(Tiles.Tile))) previewSprite.Rotation = rotation;
                    previewSprite.Opacity = 90;
                    previewSprite.Draw();
                    previewSprite.Color = System.Drawing.Color.White;
                }

                if (gameScreen.playerController.controlledAtom != null && !active.placeAnywhere)
                { 
                    Gorgon.Screen.Circle(gameScreen.playerController.controlledAtom.position.X - gameScreen.xTopLeft, gameScreen.playerController.controlledAtom.position.Y - gameScreen.yTopLeft, active.range, System.Drawing.Color.DarkBlue, 2f, 2f);
                }

                #region Debug Display
                if (previewVisible && validLocation)
                {
                    switch (active.AlignOption)
                    {
                        case AlignmentOptions.AlignSimilar:
                            if (snapToAtom != null)
                            {
                                Gorgon.Screen.Line(snapToAtom.position.X - gameScreen.xTopLeft, snapToAtom.position.Y - gameScreen.yTopLeft, -((snapToAtom.position.X - gameScreen.xTopLeft) - snapToLoc.X), -((snapToAtom.position.Y - gameScreen.yTopLeft) - snapToLoc.Y), System.Drawing.Color.White, new Vector2D(3, 3));
                                Gorgon.Screen.FilledCircle(snapToLoc.X, snapToLoc.Y, 3, System.Drawing.Color.LimeGreen);
                                Gorgon.Screen.FilledCircle(snapToAtom.position.X - gameScreen.xTopLeft, snapToAtom.position.Y - gameScreen.yTopLeft, 3, System.Drawing.Color.LimeGreen);
                            }
                            break;
                        case AlignmentOptions.AlignTile:
                            break;
                        case AlignmentOptions.AlignWall:
                            break;
                        case AlignmentOptions.AlignNone:
                            Gorgon.Screen.FilledCircle(snapToLoc.X, snapToLoc.Y, 3, System.Drawing.Color.LimeGreen);
                            break;
                    }
                }
                #endregion
            }
        }
    }
}
