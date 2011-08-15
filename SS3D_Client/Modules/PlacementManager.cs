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
using SS3D.Modules.Map;
using SS3D.Atom;

using System.Collections.Generic;
using System.Reflection;

using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

using Lidgren.Network;

using System.Windows.Forms;

namespace SS3D.Modules
{
    class GamePlacementManager
    {
        private Map.Map map;
        private AtomManager atomManager;
        private GameScreen gameScreen;

        private Type buildingType;
        private Sprite buildingSprite;

        private IEnumerable<Atom.Atom> nearbyObjsOfSameType;
        private bool listNeedsRebuilding = false;
        private Vector2D listLastPos = Vector2D.Zero;

        public System.Drawing.RectangleF buildingAABB;

        private static float buildingRange = 90;

        public bool isBuilding { get; private set; }
        public bool buildingBlocked = false;
        public bool buildingSnapTo = true;
        public bool buildingDrawRange = true;
        public bool editMode = false;

        public GamePlacementManager(Map.Map _map, AtomManager _atom, GameScreen _screen)
        {
            map = _map;
            atomManager = _atom;
            gameScreen = _screen;
            isBuilding = false;
            buildingAABB = new System.Drawing.RectangleF();
        }

        public void StartBuilding(Type type)
        {
            buildingType = type;
            buildingSprite = ResMgr.Singleton.GetSprite(atomManager.GetSpriteName(type));
            isBuilding = true;

            nearbyObjsOfSameType = from a in atomManager.atomDictionary.Values
                                           where
                                           a.visible &&
                                           System.Math.Sqrt((gameScreen.playerController.controlledAtom.position.X - a.position.X) * (gameScreen.playerController.controlledAtom.position.X - a.position.X)) < gameScreen.screenWidthTiles * map.tileSpacing + 160 &&
                                           System.Math.Sqrt((gameScreen.playerController.controlledAtom.position.Y - a.position.Y) * (gameScreen.playerController.controlledAtom.position.Y - a.position.Y)) < gameScreen.screenHeightTiles * map.tileSpacing + 160 &&
                                           buildingType == a.GetType()
                                           select a;
        }

        private bool CanPlace()
        {
            if (!editMode && (gameScreen.playerController.controlledAtom.position - gameScreen.mousePosWorld).Length > buildingRange) 
                return false;

            System.Drawing.Point arrayPos = map.GetTileArrayPositionFromWorldPosition(gameScreen.mousePosWorld);
            TileType type = map.GetTileTypeFromArrayPosition(arrayPos.X, arrayPos.Y);

            if (type == TileType.Wall) return false;

            foreach (Atom.Atom a in atomManager.atomDictionary.Values) //This is less than optimal. Dont want to loop through everything.
            {
                a.sprite.SetPosition(a.position.X - gameScreen.xTopLeft, a.position.Y - gameScreen.yTopLeft);
                a.sprite.UpdateAABB();
                if (a.sprite.AABB.IntersectsWith(buildingAABB)) return false;
            }
            return true;
        }

        public void PlaceBuilding()
        {
            if (isBuilding)
            {
                if (!editMode && !buildingBlocked && CanPlace())
                {
                    Random rnd = new Random(DateTime.Now.Hour+DateTime.Now.Minute+DateTime.Now.Millisecond);
                    Atom.Atom newObject = (Atom.Atom)Activator.CreateInstance(buildingType); //This stuff is just for testing.
                    newObject.Draw();
                    newObject.position = gameScreen.mousePosWorld;
                    newObject.atomManager = atomManager;
                    atomManager.atomDictionary[(ushort)rnd.Next(32000)] = newObject;
                    CancelBuilding();
                }
                else if (editMode && buildingType != null)
                {
                    NetOutgoingMessage message = gameScreen.prg.mNetworkMgr.netClient.CreateMessage();
                    message.Write((byte)NetMessage.AtomManagerMessage);
                    message.Write((byte)AtomManagerMessage.SpawnAtom);
                    message.Write(buildingType.FullName.Remove(0, 5));
                    message.Write(gameScreen.mousePosWorld.X);
                    message.Write(gameScreen.mousePosWorld.Y);
                    message.Write(0f);//Rotation? Doesn't seem to be used, but it was bugging out the server.
                    gameScreen.prg.mNetworkMgr.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
                    CancelBuilding();
                }
            }
        }

        public void CancelBuilding()
        {
            if (isBuilding)
            {
                buildingType = null;
                buildingSprite = null;
                isBuilding = false;
            }
        }

        public void Update()
        {
            if (editMode)
            {
                buildingType = gameScreen.prg.GorgonForm.GetAtomSpawnType();
                if (buildingType != null)
                {
                    isBuilding = true;
                    buildingSprite = ResMgr.Singleton.GetSprite(atomManager.GetSpriteName(buildingType));
                }
                else
                {
                    CancelBuilding();
                }
            }
            buildingBlocked = false;

            if (isBuilding)
            {
                System.Drawing.Point arrayPos = map.GetTileArrayPositionFromWorldPosition(gameScreen.mousePosWorld);
                TileType type = map.GetTileTypeFromArrayPosition(arrayPos.X, arrayPos.Y);

                if (type == TileType.Wall) buildingBlocked = true;

                if (gameScreen.playerController.controlledAtom != null)
                {
                    if (!editMode && (gameScreen.playerController.controlledAtom.position - gameScreen.mousePosWorld).Length > buildingRange) 
                        buildingBlocked = true;
                }

                if (buildingSprite != null)
                {
                    buildingSprite.Position = gameScreen.mousePosScreen;
                    buildingSprite.UpdateAABB();
                    buildingAABB = buildingSprite.AABB;
                }
            }
        }

        public void Draw()
        {
            if (isBuilding)
            {
                if (gameScreen.playerController.controlledAtom != null && buildingDrawRange && !editMode)
                {   //Is it a bird? A plane? No! It's a really fucking long line of code!
                    Gorgon.Screen.Circle(gameScreen.playerController.controlledAtom.position.X - gameScreen.xTopLeft, gameScreen.playerController.controlledAtom.position.Y - gameScreen.yTopLeft, buildingRange, System.Drawing.Color.DarkGreen, 2f, 2f);
                }

                if (buildingSprite != null)
                {
                    buildingSprite.Position = gameScreen.mousePosScreen;

                    if(buildingBlocked)
                        buildingSprite.Color = System.Drawing.Color.Red;
                    else
                        buildingSprite.Color = System.Drawing.Color.Green;

                    buildingSprite.Opacity = 90;
                    buildingSprite.Draw();
                }
            }
        }

        public void Shutdown()
        {
            map = null;
            atomManager = null;
            gameScreen = null;
            buildingType = null;
            buildingSprite = null;
        }
    }
}
