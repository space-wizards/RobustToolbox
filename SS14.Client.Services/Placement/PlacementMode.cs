using SFML.Graphics;
using SFML.System;
using SS14.Client.Interfaces.Map;

namespace SS14.Client.Services.Placement
{
    public class PlacementMode
    {
        public readonly PlacementManager pManager;

        public TileRef currentTile;
        public Vector2i mouseScreen;
        public Vector2f mouseWorld;
        public Sprite spriteToDraw;

        public PlacementMode(PlacementManager pMan)
        {
            pManager = pMan;
        }

        public virtual string ModeName
        {
            get { return GetType().Name; }
        }

        public virtual bool Update(Vector2i mouseScreen, IMapManager currentMap) //Return valid position?
        {
            return false;
        }

        public virtual void Render()
        {
        }

        public Sprite GetDirectionalSprite(string baseSprite)
        {
            if (baseSprite == null) return null;

            string dirName = (baseSprite + "_" + pManager.Direction.ToString()).ToLowerInvariant();
            if (pManager.ResourceManager.SpriteExists(dirName))
                return pManager.ResourceManager.GetSprite(dirName);
            else
                return null;
        }
    }
}