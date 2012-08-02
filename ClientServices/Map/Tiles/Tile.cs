using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ClientInterfaces;
using ClientInterfaces.Lighting;
using ClientInterfaces.Map;
using ClientInterfaces.Resource;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using SS13_Shared;

namespace ClientServices.Map.Tiles
{
    public abstract class Tile : ITile
    {
        public TileType TileType { get; protected set; }
        public TileState tileState = TileState.Healthy;
        public string name;
        protected Sprite Sprite;
        public Sprite sideSprite;
        public Sprite lightSprite;
        public Vector2D Position { get; protected set; }
        public Point TilePosition { get; protected set; }
        public bool Visible { get; set; }
        public bool sightBlocked = false; // Is something on this tile that blocks sight through it, like a door (used for lighting)
        public byte surroundDirs = 0; //north = 1 east = 2 south = 4 west = 8.
        public Tile[] surroundingTiles;
        //public Dictionary<VertexLocations, bool> vertexVisibility;
        public List<ILight> tileLights;
        public Dictionary<GasType, int> gasAmounts;
        public Sprite gasSprite;
        public List<TileDecal> decals;
        private Random _random;
        private readonly ILightManager _lightManager;
        private readonly IResourceManager _resourceManager;

        protected Tile(Sprite sprite, TileState state, float size, Vector2D position, Point tilePosition, ILightManager lightManager, IResourceManager resourceManager)
        {
            TileType = TileType.None;
            tileState = state;
            Position = position;
            TilePosition = tilePosition;
            Sprite = sprite;
            Sprite.SetPosition(position.X, position.Y);

            _lightManager = lightManager;
            _resourceManager = resourceManager;

            Initialize();
        }

        protected Tile(Sprite sprite, Sprite _side, TileState state, float size, Vector2D _position, Point _tilePosition, ILightManager lightManager, IResourceManager resourceManager)
        {
            TileType = TileType.None;
            tileState = state;
            Position = _position;
            TilePosition = _tilePosition;
            Sprite = sprite;
            Sprite.SetPosition(_position.X, _position.Y);

            sideSprite = _side;
            sideSprite.SetPosition(_position.X, _position.Y);

            _lightManager = lightManager;
            _resourceManager = resourceManager;

            Initialize();
        }

        public virtual void Initialize()
        {
            gasSprite = _resourceManager.GetSprite("gas");
            surroundingTiles = new Tile[4];
            tileLights = new List<ILight>();
            gasAmounts = new Dictionary<GasType, int>();
            sightBlocked = false;
            decals = new List<TileDecal>();
            _random = new Random((int)(Position.X * Position.Y));
            lightSprite = _resourceManager.GetSprite("white");
            Visible = true;


        }

        public void SetSprites(Sprite _sprite, Sprite _side, byte _surroundDirs)
        {
            Sprite = _sprite;
            sideSprite = _side;
            surroundDirs = _surroundDirs;
        }

        public virtual void Render(float xTopLeft, float yTopLeft, int tileSpacing, Batch batch)
        {
            Sprite.Color = Color.White;
            Sprite.SetPosition((float)TilePosition.X * tileSpacing - xTopLeft, (float)TilePosition.Y * tileSpacing - yTopLeft);
            //Sprite.Draw();
            batch.AddClone(Sprite);
        }

        public virtual void RenderPos(float x, float y, int tileSpacing, int lightSize)
        {
            Sprite.Color = Color.Transparent;
            Sprite.SetPosition(x, y);
            Sprite.Draw();
        }

        public virtual void RenderPosOffset(float x, float y, int tileSpacing, Vector2D lightPosition)
        {
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
                    decals.Add(new TileDecal(_resourceManager.GetSprite(decalname), new Vector2D(_random.Next(0, 64), _random.Next(0, 64)), this, System.Drawing.Color.FromArgb(165, 6, 6)));
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
                    if (gasAmount.Value <= 1)
                        continue;
                    if (!spritepositionset)
                    {
                        gasSprite.SetPosition(TilePosition.X * tileSpacing - xTopLeft, TilePosition.Y * tileSpacing - yTopLeft);
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
            //FIXTHIS
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
            sprite.SetPosition(tile.TilePosition.X * tileSpacing - xTopLeft + position.X, tile.TilePosition.Y * tileSpacing - yTopLeft + position.Y);
            decalBatch.AddClone(sprite);
        }
    }
}