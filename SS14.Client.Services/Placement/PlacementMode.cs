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

        public Sprite GetSprite(string key)
        {
            if (key == null) return null;

            if (pManager.ResourceManager.SpriteExists(key))
                return pManager.ResourceManager.GetSprite(key);
            else
                return null;
        }

        public Sprite GetDirectionalSprite(string baseSprite)
        {
            if (baseSprite == null) return null;

            return GetSprite((baseSprite + "_" + pManager.Direction.ToString()).ToLowerInvariant());
        }
    }
}