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
using SS3D.Atom;

using System.Collections.Generic;
using System.Reflection;

using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

using Lidgren.Network;

using System.Windows.Forms;
using SS3D_shared.HelperClasses;
using SS3D.Atom;

namespace SS3D.Modules
{
    class PlacementManager
    {
        private Map.Map map;
        private AtomManager atomManager;
        private GameScreen gameScreen;

        BuildPermission active;

        Sprite previewSprite;
        Type activeType;
        Atom.Atom snapToAtom; //The current atom for snap-to-similar
        byte snapToSide;      //The current side of the current atom for snap-to-similar. 1 Top, 2 Right, 3 Bottom, 4 Left. Unused.
        Boolean validLocation = false;
        Boolean previewVisible = true;

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

        public void Initialize(Map.Map _map, AtomManager _atom, GameScreen _screen)
        {
            map = _map;
            atomManager = _atom;
            gameScreen = _screen;
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
                    break;
                case PlacementManagerMessage.PlacementFailed:
                    break;
            }
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

        private void SetupPlacement()
        {
            Assembly currentAssembly = Assembly.GetExecutingAssembly();
            Type atomType = currentAssembly.GetType("SS3D." + active.type);
            activeType = atomType;
            previewSprite = ResMgr.Singleton.GetSprite(GetSpriteName(atomType));
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
                        if (type == TileType.Wall) validLocation = false; //TO-DO: Better way for wall-checks. See byond density.

                        var atomsBlocking = from a in atomManager.atomDictionary.Values
                                            where a.collidable
                                            where a.GetAABB().IntersectsWith(previewSprite.AABB) || a.GetAABB().IntersectsWith(new System.Drawing.RectangleF(gameScreen.mousePosWorld.X,gameScreen.mousePosWorld.Y,2,2))
                                            select a;

                        if (atomsBlocking.Any() || (gameScreen.mousePosWorld - gameScreen.playerController.controlledAtom.position).Length > active.range)
                            validLocation = false;
                        break; 
                    #endregion

                    #region Align Similar
                    case AlignmentOptions.AlignSimilar:
                        var atoms = from a in atomManager.atomDictionary.Values
                                    where a.IsTypeOf(activeType)
                                    where a.visible
                                    where (a.position - gameScreen.mousePosWorld).Length <= active.range * 2
                                    where (a.position - gameScreen.playerController.controlledAtom.position).Length <= active.range
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
                                              where (vec - gameScreen.playerController.controlledAtom.position).Length <= active.range
                                              orderby (vec - gameScreen.mousePosWorld).Length ascending
                                              select vec;

                            if (closestSide.Any())
                            {
                                snapToLoc = new Vector2D(closestSide.First().X - gameScreen.xTopLeft, closestSide.First().Y - gameScreen.yTopLeft);
                            }
                            else //No side in range. This shouldnt be possible if the object itself is in range.
                            {
                                validLocation = false;
                                previewVisible = false;
                            }
                            snapToAtom = atoms.First();
                        }
                        else //Nothing in range.
                        {
                            validLocation = false;
                            previewVisible = false;
                        }
                        break; 
                    #endregion

                    #region Align Wall
                    case AlignmentOptions.AlignWall:
                        Tiles.Tile wall = map.GetTileAt(gameScreen.mousePosWorld);
                        if (wall.tileType == TileType.Wall) //TODO: Needs a better way of finding solid tiles. See byond density.
                        {
                            Vector2D Node1 = new Vector2D(gameScreen.mousePosWorld.X, wall.position.Y);//Bit ugly.
                            Vector2D Node2 = new Vector2D(gameScreen.mousePosWorld.X, wall.position.Y + 16);
                            Vector2D Node3 = new Vector2D(gameScreen.mousePosWorld.X, wall.position.Y + 32);
                            Vector2D Node4 = new Vector2D(gameScreen.mousePosWorld.X, wall.position.Y + 48);

                            List<Vector2D> Nodes = new List<Vector2D>();
                            Nodes.Add(Node1);
                            Nodes.Add(Node2);
                            Nodes.Add(Node3);
                            Nodes.Add(Node4);

                            var closestNode = from Vector2D vec in Nodes
                                              where (vec - gameScreen.playerController.controlledAtom.position).Length <= active.range
                                              orderby (vec - gameScreen.mousePosWorld).Length ascending
                                              select vec;

                            if (closestNode.Any())
                            {
                                snapToLoc = new Vector2D(closestNode.First().X - gameScreen.xTopLeft, closestNode.First().Y - gameScreen.yTopLeft);
                            }
                            else //No node in range. This shouldnt be possible if the object itself is in range.
                            {
                                validLocation = false;
                                previewVisible = false;
                            }
                        }
                        else //Not a supported tile. Or not in range.
                        {
                            validLocation = false;
                            previewVisible = false;
                        }
                        break;
                    #endregion

                    #region Align Tile
                    case AlignmentOptions.AlignTile:
                        Tiles.Tile tile = map.GetTileAt(gameScreen.mousePosWorld);
                        snapToLoc = new Vector2D(tile.position.X + (map.tileSpacing / 2) - gameScreen.xTopLeft, tile.position.Y + (map.tileSpacing / 2) - gameScreen.yTopLeft);
                        if((snapToLoc - gameScreen.playerController.controlledAtom.position).Length > active.range) validLocation = false;
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
                    previewSprite.Opacity = 90;
                    previewSprite.Draw();
                    previewSprite.Color = System.Drawing.Color.White;
                }

                if (gameScreen.playerController.controlledAtom != null)
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

    //class GamePlacementManager
    //{
    //    private Map.Map map;
    //    private AtomManager atomManager;
    //    private GameScreen gameScreen;

    //    private Type buildingType;
    //    private Sprite buildingSprite;

    //    private IEnumerable<Atom.Atom> nearbyObjsOfSameType;
    //    private bool listNeedsRebuilding = false;
    //    private Vector2D listLastPos = Vector2D.Zero;

    //    public System.Drawing.RectangleF buildingAABB;

    //    private static float buildingRange = 90;

    //    public bool isBuilding { get; private set; }
    //    public bool buildingBlocked = false;
    //    public bool buildingSnapTo = true;
    //    public bool buildingDrawRange = true;
    //    public bool editMode = false;

    //    public GamePlacementManager(Map.Map _map, AtomManager _atom, GameScreen _screen)
    //    {
    //        map = _map;
    //        atomManager = _atom;
    //        gameScreen = _screen;
    //        isBuilding = false;
    //        buildingAABB = new System.Drawing.RectangleF();
    //    }

    //    public void StartBuilding(Type type)
    //    {
    //        buildingType = type;
    //        buildingSprite = ResMgr.Singleton.GetSprite(atomManager.GetSpriteName(type));
    //        isBuilding = true;

    //        nearbyObjsOfSameType = from a in atomManager.atomDictionary.Values
    //                                       where
    //                                       a.visible &&
    //                                       System.Math.Sqrt((gameScreen.playerController.controlledAtom.position.X - a.position.X) * (gameScreen.playerController.controlledAtom.position.X - a.position.X)) < gameScreen.screenWidthTiles * map.tileSpacing + 160 &&
    //                                       System.Math.Sqrt((gameScreen.playerController.controlledAtom.position.Y - a.position.Y) * (gameScreen.playerController.controlledAtom.position.Y - a.position.Y)) < gameScreen.screenHeightTiles * map.tileSpacing + 160 &&
    //                                       buildingType == a.GetType()
    //                                       select a;
    //    }

    //    private bool CanPlace()
    //    {
    //        if (!editMode && (gameScreen.playerController.controlledAtom.position - gameScreen.mousePosWorld).Length > buildingRange) 
    //            return false;

    //        System.Drawing.Point arrayPos = map.GetTileArrayPositionFromWorldPosition(gameScreen.mousePosWorld);
    //        TileType type = map.GetTileTypeFromArrayPosition(arrayPos.X, arrayPos.Y);

    //        if (type == TileType.Wall) return false;

    //        foreach (Atom.Atom a in atomManager.atomDictionary.Values) //This is less than optimal. Dont want to loop through everything.
    //        {
    //            a.sprite.SetPosition(a.position.X - gameScreen.xTopLeft, a.position.Y - gameScreen.yTopLeft);
    //            a.sprite.UpdateAABB();
    //            if (a.sprite.AABB.IntersectsWith(buildingAABB)) return false;
    //        }
    //        return true;
    //    }

    //    public void PlaceBuilding()
    //    {
    //        if (isBuilding)
    //        {
    //            if (!editMode && !buildingBlocked && CanPlace())
    //            {
    //                Random rnd = new Random(DateTime.Now.Hour+DateTime.Now.Minute+DateTime.Now.Millisecond);
    //                Atom.Atom newObject = (Atom.Atom)Activator.CreateInstance(buildingType); //This stuff is just for testing.
    //                newObject.Draw();
    //                newObject.position = gameScreen.mousePosWorld;
    //                newObject.atomManager = atomManager;
    //                atomManager.atomDictionary[(ushort)rnd.Next(32000)] = newObject;
    //                CancelBuilding();
    //            }
    //            else if (editMode && buildingType != null)
    //            {
    //                NetOutgoingMessage message = gameScreen.prg.mNetworkMgr.netClient.CreateMessage();
    //                message.Write((byte)NetMessage.AtomManagerMessage);
    //                message.Write((byte)AtomManagerMessage.SpawnAtom);
    //                message.Write(buildingType.FullName.Remove(0, 5));
    //                message.Write(gameScreen.mousePosWorld.X);
    //                message.Write(gameScreen.mousePosWorld.Y);
    //                message.Write(0f);//Rotation? Doesn't seem to be used, but it was bugging out the server.
    //                gameScreen.prg.mNetworkMgr.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
    //                CancelBuilding();
    //            }
    //        }
    //    }

    //    public void CancelBuilding()
    //    {
    //        if (isBuilding)
    //        {
    //            buildingType = null;
    //            buildingSprite = null;
    //            isBuilding = false;
    //        }
    //    }

    //    public void Update()
    //    {
    //        if (editMode)
    //        {
    //            buildingType = gameScreen.prg.GorgonForm.GetAtomSpawnType();
    //            if (buildingType != null)
    //            {
    //                isBuilding = true;
    //                buildingSprite = ResMgr.Singleton.GetSprite(atomManager.GetSpriteName(buildingType));
    //            }
    //            else
    //            {
    //                CancelBuilding();
    //            }
    //        }
    //        buildingBlocked = false;

    //        if (isBuilding)
    //        {
    //            System.Drawing.Point arrayPos = map.GetTileArrayPositionFromWorldPosition(gameScreen.mousePosWorld);
    //            TileType type = map.GetTileTypeFromArrayPosition(arrayPos.X, arrayPos.Y);

    //            if (type == TileType.Wall) buildingBlocked = true;

    //            if (gameScreen.playerController.controlledAtom != null)
    //            {
    //                if (!editMode && (gameScreen.playerController.controlledAtom.position - gameScreen.mousePosWorld).Length > buildingRange) 
    //                    buildingBlocked = true;
    //            }

    //            if (buildingSprite != null)
    //            {
    //                buildingSprite.Position = gameScreen.mousePosScreen;
    //                buildingSprite.UpdateAABB();
    //                buildingAABB = buildingSprite.AABB;
    //            }
    //        }
    //    }

    //    public void Draw()
    //    {
    //        if (isBuilding)
    //        {
    //            if (gameScreen.playerController.controlledAtom != null && buildingDrawRange && !editMode)
    //            {   //Is it a bird? A plane? No! It's a really fucking long line of code!
    //                Gorgon.Screen.Circle(gameScreen.playerController.controlledAtom.position.X - gameScreen.xTopLeft, gameScreen.playerController.controlledAtom.position.Y - gameScreen.yTopLeft, buildingRange, System.Drawing.Color.DarkGreen, 2f, 2f);
    //            }

    //            if (buildingSprite != null)
    //            {
    //                buildingSprite.Position = gameScreen.mousePosScreen;

    //                if(buildingBlocked)
    //                    buildingSprite.Color = System.Drawing.Color.Red;
    //                else
    //                    buildingSprite.Color = System.Drawing.Color.Green;

    //                buildingSprite.Opacity = 90;
    //                buildingSprite.Draw();
    //            }
    //        }
    //    }

    //    public void Shutdown()
    //    {
    //        map = null;
    //        atomManager = null;
    //        gameScreen = null;
    //        buildingType = null;
    //        buildingSprite = null;
    //    }
    //}
}
