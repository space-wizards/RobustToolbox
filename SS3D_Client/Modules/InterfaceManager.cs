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

        public System.Drawing.RectangleF buildingAABB;

        private static float buildingRange = 64; //It's a bad idea to set this higher than tiles are wide.

        public bool isBuilding { get; private set; }
        public bool buildingBlocked = false;
        public bool buildingSnapTo = true;
        public bool buildingDrawRange = true;

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
        }

        private bool CanPlace()
        {
            if ((gameScreen.playerController.controlledAtom.position - gameScreen.mousePosWorld).Length > buildingRange) return false;

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
                if (!buildingBlocked && CanPlace())
                {
                    Random rnd = new Random(DateTime.Now.Hour+DateTime.Now.Minute+DateTime.Now.Millisecond);
                    Atom.Atom newObject = (Atom.Atom)Activator.CreateInstance(buildingType); //This stuff is just for testing.
                    newObject.Draw();
                    newObject.position = gameScreen.mousePosWorld;
                    newObject.atomManager = atomManager;
                    atomManager.atomDictionary[(ushort)rnd.Next(32000)] = newObject;
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
            buildingBlocked = false;

            if (isBuilding)
            {
                System.Drawing.Point arrayPos = map.GetTileArrayPositionFromWorldPosition(gameScreen.mousePosWorld);
                TileType type = map.GetTileTypeFromArrayPosition(arrayPos.X, arrayPos.Y);

                if (type == TileType.Wall) buildingBlocked = true;

                if (gameScreen.playerController.controlledAtom != null)
                {
                    if ((gameScreen.playerController.controlledAtom.position - gameScreen.mousePosWorld).Length > buildingRange) 
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
                if (gameScreen.playerController.controlledAtom != null && buildingDrawRange)
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
