using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using System.Drawing;
using SS3D.Modules;

namespace SS3D.Tiles
{
    public abstract class Tile
    {
        public TileType tileType = TileType.None;
        public TileState tileState = TileState.Healthy;
        public string name;
        public Sprite sprite;
        public Sprite sideSprite;
        public Vector2D position;
        public Point tilePosition;
        public bool Visible = false;
        public bool sightBlocked = false; // Is something on this tile that blocks sight through it, like a door (used for lighting)
        public byte surroundDirs = 0;
        public Tile[] surroundingTiles;
        //public Dictionary<VertexLocations, bool> vertexVisibility;
        public List<Light> tileLights;
        public Dictionary<GasType, int> gasAmounts;
        public Sprite gasSprite;
        public List<TileDecal> decals;
        private Random _random;

        public Tile(Sprite _sprite, TileState state, float size, Vector2D _position, Point _tilePosition)
        {
            tileState = state;
            position = _position;
            tilePosition = _tilePosition;
            sprite = _sprite;
            sprite.SetPosition(_position.X, _position.Y);

            Initialize();
        }

        public Tile(Sprite _sprite, Sprite _side, TileState state, float size, Vector2D _position, Point _tilePosition)
        {
            tileState = state;
            position = _position;
            tilePosition = _tilePosition;
            sprite = _sprite;
            sprite.SetPosition(_position.X, _position.Y);

            sideSprite = _side;
            sideSprite.SetPosition(_position.X, _position.Y);

            Initialize();
        }

        public void Initialize()
        {
            gasSprite = ResMgr.Singleton.GetSpriteFromImage("gas");
            surroundingTiles = new Tile[4];
            tileLights = new List<Light>();
            gasAmounts = new Dictionary<GasType, int>();
            sightBlocked = false;
            decals = new List<TileDecal>();
            _random = new Random((int)(position.X * position.Y));
        }

        public void SetSprites(Sprite _sprite, Sprite _side, byte _surroundDirs)
        {
            sprite = _sprite;
            sideSprite = _side;
            surroundDirs = _surroundDirs;
        }

        public virtual void Render(float xTopLeft, float yTopLeft, int tileSpacing)
        {
            if (Visible)
            {
                sprite.Color = Color.White;
                sprite.SetPosition(tilePosition.X * tileSpacing - xTopLeft, tilePosition.Y * tileSpacing - yTopLeft);
                LightManager.Singleton.ApplyLightsToSprite(tileLights, sprite, new Vector2D(xTopLeft, yTopLeft));
                sprite.Draw();
            }
        }

        public virtual void DrawDecals(float xTopLeft, float yTopLeft, int tileSpacing, Batch decalBatch)
        {
            foreach (TileDecal d in decals)
            {
                d.Draw(xTopLeft, yTopLeft, tileSpacing, decalBatch);
            }
        }

        public void AddDecal(DecalType type)
        {
            switch (type)
            {
                case DecalType.Blood:
                    string decalname;
                    switch (_random.Next(1, 4))
                    {
                        case 1:
                            decalname = "spatter_decal";
                            break;
                        case 2:
                            decalname = "spatter_decal2";
                            break;
                        case 3:
                            decalname = "spatter_decal3";
                            break;
                        default:
                            decalname = "spatter_decal4";
                            break;
                    }
                    decals.Add(new TileDecal(ResMgr.Singleton.GetSprite(decalname), new Vector2D(_random.Next(0, 64), _random.Next(0, 64)), this, System.Drawing.Color.FromArgb(165,6,6)));
                    break;

            }
        }
        public virtual void RenderGas(float xTopLeft, float yTopLeft, int tileSpacing, Batch gasBatch)
        {
            if (Visible && gasAmounts.Count > 0)
            {
                bool spritepositionset = false;
                foreach (var gasAmount in gasAmounts)
                {
                    if (gasAmount.Value <= 1) //Meh.
                        continue;
                    if (!spritepositionset)
                    {
                        gasSprite.SetPosition(tilePosition.X * tileSpacing - xTopLeft, tilePosition.Y * tileSpacing - yTopLeft);
                        spritepositionset = true;
                    }
                
                    int opacity = (int)Math.Floor(((double)gasAmount.Value / 15) * 255);
                    
                    switch (gasAmount.Key)
                    {
                        case GasType.HighVel:
                            gasSprite.Color = Color.FromArgb(opacity, Color.White);
                            break;
                        case GasType.Toxin:
                            gasSprite.Color = Color.FromArgb(opacity, Color.Orange);
                            break;
                        case GasType.WVapor:
                            gasSprite.Color = Color.FromArgb(opacity, Color.LightBlue);
                            break;
                    }
                    gasBatch.AddClone(gasSprite);
                    //gasSprite.Draw();//UGH THIS IS SLOW AS FUCK
                }
            }
        }

        public virtual void RenderTop(float xTopLeft, float yTopLeft, int tileSpacing, Batch wallTopsBatch)
        {
        }

        public virtual void SetAtmosDisplay(byte displayByte)
        {
            int _type = displayByte >> 4;
            GasType type = (GasType)_type;
            int amount = displayByte & 15;

            if (gasAmounts.Keys.Contains(type))
                gasAmounts[type] = amount;
            else
                gasAmounts.Add(type, amount);
        }
    }

    public class TileDecal
    {
        public Sprite sprite;
        public Vector2D position; // Position relative to top left corner of tile
        public Tile tile;

        public TileDecal(Sprite _sprite, Vector2D _position, Tile _tile, System.Drawing.Color color)
        {
            sprite = _sprite;
            position = _position;
            tile = _tile;
            sprite.Color = color;
        }

        public void Draw(float xTopLeft, float yTopLeft, int tileSpacing, Batch decalBatch)
        {
            //Need to find a way to light it.
            sprite.SetPosition(tile.tilePosition.X * tileSpacing - xTopLeft + position.X, tile.tilePosition.Y * tileSpacing - yTopLeft + position.Y);
            decalBatch.AddClone(sprite);
        }
    }
}