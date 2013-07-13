using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClientInterfaces.Map;
using ClientServices.Resources;
using GorgonLibrary;
using GorgonLibrary.Graphics;

namespace ClientServices.Placement
{
    public class PlacementMode
    {
        public readonly PlacementManager pManager;

        public Sprite spriteToDraw;
        public Vector2D mouseWorld;
        public Vector2D mouseScreen;
        public ITile currentTile;

        public virtual string ModeName
        {
            get { return this.GetType().Name; }
        }

        public PlacementMode(PlacementManager pMan)
        {
            pManager = pMan;
        }

        public virtual bool Update(Vector2D mouseScreen, IMapManager currentMap) //Return valid position?
        {
            return false;
        }

        public virtual void Render()
        {
        }

        public Sprite GetDirectionalSprite(Sprite baseSprite)
        {
            Sprite spriteToUse = baseSprite;

            if (baseSprite == null) return null;

            string dirName = (baseSprite.Name + "_" + pManager.Direction.ToString()).ToLowerInvariant();
            if (pManager.ResourceManager.SpriteExists(dirName))
                spriteToUse = pManager.ResourceManager.GetSprite(dirName);

            return spriteToUse;
        }
    }
}
