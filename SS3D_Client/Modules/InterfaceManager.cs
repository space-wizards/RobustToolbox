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
    class GameInterfaceManager
    {
        private Map.Map map;
        private AtomManager atomManager;
        private GameScreen gameScreen;

        private Type buildingType;
        private Sprite buildingSprite;
        private Vector2D buildingPositionWorld;

        public System.Drawing.RectangleF buildingAABB;

        private static float buildingRange = 64; //It's a bad idea to set this higher than tiles are wide.

        public bool isBuilding { get; private set; }
        public bool buildingBlocked = false;
        public bool buildingSnapTo = true;
        public bool buildingSnapToGrid = false;
        public bool buildingDrawRange = true;
        public bool editMode = false;

        public GameInterfaceManager(Map.Map _map, AtomManager _atom, GameScreen _screen)
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
            buildingSnapToGrid = atomManager.GetSnapToGrid(type);
            buildingPositionWorld = gameScreen.mousePosWorld;
        }

        private bool CanPlace()
        {
            if (!editMode && (gameScreen.playerController.controlledAtom.position - buildingPositionWorld).Length > buildingRange) 
                return false;

            System.Drawing.Point arrayPos = map.GetTileArrayPositionFromWorldPosition(buildingPositionWorld);
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
                    newObject.position = buildingPositionWorld;
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
                    message.Write(buildingPositionWorld.X);
                    message.Write(buildingPositionWorld.Y);
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
            buildingPositionWorld = gameScreen.mousePosWorld;
            if (editMode)
            {
                if (buildingType != gameScreen.prg.GorgonForm.GetAtomSpawnType())
                {
                    buildingType = gameScreen.prg.GorgonForm.GetAtomSpawnType();
                    if (buildingType != null)
                    {
                        isBuilding = true;
                        buildingSprite = ResMgr.Singleton.GetSprite(atomManager.GetSpriteName(buildingType));
                        buildingSnapToGrid = atomManager.GetSnapToGrid(buildingType);
                    }
                    else
                    {
                        CancelBuilding();
                    }
                }
            }
            buildingBlocked = false;

            if (isBuilding)
            {
                System.Drawing.Point arrayPos = map.GetTileArrayPositionFromWorldPosition(buildingPositionWorld);
                TileType type = map.GetTileTypeFromArrayPosition(arrayPos.X, arrayPos.Y);

                if (buildingSnapToGrid)
                {
                    buildingPositionWorld = new Vector2D(arrayPos.X * 64, arrayPos.Y * 64);
                }

                if (type == TileType.Wall) buildingBlocked = true;

                if (gameScreen.playerController.controlledAtom != null)
                {
                    if (!editMode && (gameScreen.playerController.controlledAtom.position - buildingPositionWorld).Length > buildingRange) 
                        buildingBlocked = true;
                }

                if (buildingSprite != null)
                {
                    buildingSprite.Position = buildingPositionWorld - new Vector2D(gameScreen.xTopLeft, gameScreen.yTopLeft);
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
                    if (buildingSnapToGrid)
                    {
                        System.Drawing.Point arrayPos = map.GetTileArrayPositionFromWorldPosition(buildingPositionWorld);
                        buildingPositionWorld = new Vector2D(arrayPos.X * map.tileSpacing + (buildingSprite.Width / 2), arrayPos.Y * map.tileSpacing);
                    }
                    buildingSprite.Position = buildingPositionWorld - new Vector2D(gameScreen.xTopLeft, gameScreen.yTopLeft);


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
