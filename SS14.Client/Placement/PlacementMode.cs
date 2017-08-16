using SFML.Graphics;
using SFML.System;
using SS14.Client.Graphics;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Map;
using Vector2i = SFML.System.Vector2i;

namespace SS14.Client.Placement
{
    public class PlacementMode
    {
        public readonly PlacementManager pManager;

        public TileRef currentTile;
        public Vector2i mouseScreen;
        public Vector2 mouseWorld;
        public Sprite spriteToDraw;
        public Color validPlaceColor = new Color(34, 139, 34); //Default valid color is green
        public Color invalidPlaceColor = new Color(34, 34, 139); //Default invalid placement is red

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
            if (spriteToDraw != null)
            {
                var bounds = spriteToDraw.GetLocalBounds();
                spriteToDraw.Color = pManager.ValidPosition ? validPlaceColor : invalidPlaceColor;
                spriteToDraw.Position = new Vector2(mouseScreen.X - (bounds.Width / 2f),
                                                     mouseScreen.Y - (bounds.Height / 2f));
                //Centering the sprite on the cursor.
                spriteToDraw.Draw();
            }
        }

        public Sprite GetSprite(string key)
        {
            if (key == null || !pManager.ResourceCache.SpriteExists(key))
            {
                return pManager.ResourceCache.DefaultSprite();
            }
            else
            {
                return pManager.ResourceCache.GetSprite(key);
            }
        }

        public Sprite GetDirectionalSprite(string baseSprite)
        {
            if (baseSprite == null) pManager.ResourceCache.DefaultSprite();

            return GetSprite((baseSprite + "_" + pManager.Direction.ToString()).ToLowerInvariant());
        }
    }
}