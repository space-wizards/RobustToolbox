using SFML.Graphics;
using SFML.System;
using SS14.Client.Interfaces.Map;
using System.Text;

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
            if (key == null || !pManager.ResourceManager.SpriteExists(key))
            {
                return pManager.ResourceManager.GetNoSprite();
            }
            else
            {
                return pManager.ResourceManager.GetSprite(key);
            }
        }

        public Sprite GetDirectionalSprite(string baseSprite)
        {
            if (baseSprite == null) pManager.ResourceManager.GetNoSprite();

            string directionStr = pManager.Direction.ToString();
            StringBuilder sb = new StringBuilder(baseSprite.Length + 1 + directionStr.Length);
            sb.Append(baseSprite);
            sb.Append('_');
            sb.Append(directionStr);
            return GetSprite(sb.ToString().ToLowerInvariant());
        }
    }
}