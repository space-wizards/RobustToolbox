using GorgonLibrary;
using GorgonLibrary.Graphics;
using SS14.Client.Interfaces.Map;

namespace SS14.Client.Services.Placement
{
    public class PlacementMode
    {
        public readonly PlacementManager pManager;

        public TileRef currentTile;
        public Vector2D mouseScreen;
        public Vector2D mouseWorld;
        public Sprite spriteToDraw;

        public PlacementMode(PlacementManager pMan)
        {
            pManager = pMan;
        }

        public virtual string ModeName
        {
            get { return GetType().Name; }
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