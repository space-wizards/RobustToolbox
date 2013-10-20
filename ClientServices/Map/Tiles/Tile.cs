using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ClientInterfaces.Lighting;
using ClientInterfaces.Map;
using ClientInterfaces.Resource;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using SS13.IoC;
using SS13_Shared;

namespace ClientServices.Tiles
{
    public abstract class Tile : ITile, IQuadObject
    {
        protected readonly ILightManager _lightManager;
        protected readonly IResourceManager _resourceManager;
        protected Sprite Sprite;
        protected Random _random;
        public List<TileDecal> decals;
        public Dictionary<GasType, int> gasAmounts;
        public Sprite gasSprite;
        public string name;
        public Sprite sideSprite;
        public byte surroundDirs = 0; //north = 1 east = 2 south = 4 west = 8.
        public Tile[] surroundingTiles;
        public TileState tileState = TileState.Healthy;

        protected Tile(TileState state, Vector2D position)
        {
            _resourceManager = IoCManager.Resolve<IResourceManager>();
            _lightManager = IoCManager.Resolve<ILightManager>();

            tileState = state;

            Position = position;

            Sprite = _resourceManager.GetSprite("space_texture");
            Sprite.SetPosition(position.X, position.Y);

            Initialize();
        }

        #region ITile Members

        public Vector2D Position { get; protected set; }
        public bool Visible { get; set; }

        public bool Opaque { get; set; } //Does this block LOS etc?
        public bool ConnectSprite { get; set; }
        //Should this tile cause things like walls to change their sprite to 'connect' to this tile?

        public virtual void Render(float xTopLeft, float yTopLeft, int tileSpacing, Batch batch)
        {
            Sprite.Color = Color.White;
            Sprite.SetPosition((float)Position.X - xTopLeft,
                               (float)Position.Y - yTopLeft);
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
                        gasSprite.SetPosition(Position.X - xTopLeft,
                                              Position.Y - yTopLeft);
                        spritepositionset = true;
                    }

                    //int opacity = (int)Math.Floor(((double)gasAmount.Value / 15) * 255); // Commenting this out for now as it just makes some gas invisible until there's a shit load of it
                    int opacity = 255;

                    switch (gasAmount.Key)
                    {
                        case GasType.Toxin:
                            gasSprite.Color = Color.FromArgb(opacity, Color.Orange);
                            break;
                        case GasType.WVapor:
                            gasSprite.Color = Color.FromArgb(opacity, Color.LightBlue);
                            break;
                    }
                    gasBatch.AddClone(gasSprite);
                }
            }
        }

        public virtual void RenderTop(float xTopLeft, float yTopLeft, int tileSpacing, Batch wallTopsBatch)
        {
            //FIXTHIS
        }

        public bool IsSolidTile()
        {
            Tile tile = this;
            if (tile.GetType().GetInterface("ICollidable") != null)
                return true;
            else return false;
        }

        #endregion

        public virtual void Initialize()
        {
            gasSprite = _resourceManager.GetSprite("gas");
            surroundingTiles = new Tile[4];
            gasAmounts = new Dictionary<GasType, int>();
            decals = new List<TileDecal>();
            _random = new Random((int) (Position.X*Position.Y));

            Visible = true;
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
                    decals.Add(new TileDecal(_resourceManager.GetSprite(decalname),
                                             new Vector2D(_random.Next(0, 64), _random.Next(0, 64)), this,
                                             Color.FromArgb(165, 6, 6)));
                    break;
            }
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

        public RectangleF Bounds
        {
            get { return new RectangleF(Position.X, Position.Y, 64f, 64f); }
        }
    }

    public class TileDecal
    {
        public Vector2D position; // Position relative to top left corner of tile
        public Sprite sprite;
        public Tile tile;

        public TileDecal(Sprite _sprite, Vector2D _position, Tile _tile, Color color)
        {
            sprite = _sprite;
            position = _position;
            tile = _tile;
            sprite.Color = color;
        }

        public void Draw(float xTopLeft, float yTopLeft, int tileSpacing, Batch decalBatch)
        {
            //Need to find a way to light it.
            sprite.SetPosition(tile.Position.X - xTopLeft + position.X,
                               tile.Position.Y - yTopLeft + position.Y);
            decalBatch.AddClone(sprite);
        }
    }
}