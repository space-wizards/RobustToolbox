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
using ClientServices.Resources;
using SS13.IoC;

namespace ClientServices.Tiles
{
    public abstract class Tile : ITile
    {
        public TileState tileState = TileState.Healthy;
        public string name;
        protected Sprite Sprite;
        public Sprite sideSprite;

        public Vector2D Position { get; protected set; }
        public Point TilePosition { get; protected set; }
        public bool Visible { get; set; }
        public byte surroundDirs = 0; //north = 1 east = 2 south = 4 west = 8.
        public Tile[] surroundingTiles;
        public Dictionary<GasType, int> gasAmounts;
        public Sprite gasSprite;
        public List<TileDecal> decals;
        protected Random _random;
        protected readonly ILightManager _lightManager;
        protected readonly IResourceManager _resourceManager;

        public bool Opaque { get; set; } //Does this block LOS etc?
        public bool ConnectSprite { get; set; } //Should this tile cause things like walls to change their sprite to 'connect' to this tile?

        protected Tile(TileState state, Vector2D position, Point tilePosition)
        {
            _resourceManager = IoCManager.Resolve<IResourceManager>();
            _lightManager = IoCManager.Resolve<ILightManager>();

            tileState = state;

            Position = position;
            TilePosition = tilePosition;

            Sprite = _resourceManager.GetSprite("space_texture");
            Sprite.SetPosition(position.X, position.Y);

            Initialize();
        }

        public virtual void Initialize()
        {
            gasSprite = _resourceManager.GetSprite("gas");
            surroundingTiles = new Tile[4];
            gasAmounts = new Dictionary<GasType, int>();
            decals = new List<TileDecal>();
            _random = new Random((int)(Position.X * Position.Y));

            Visible = true;
        }

        public virtual void Render(float xTopLeft, float yTopLeft, int tileSpacing, Batch batch)
        {
            Sprite.Color = Color.White;
            Sprite.SetPosition((float)TilePosition.X * tileSpacing - xTopLeft, (float)TilePosition.Y * tileSpacing - yTopLeft);
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

        public virtual void SetAtmosDisplay(GasType type, byte amount)
        {
            if (gasAmounts.Keys.Contains(type))
            {
                if (amount == 0)
                    gasAmounts.Remove(type);
                else
                    gasAmounts[type] = amount;
            }
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