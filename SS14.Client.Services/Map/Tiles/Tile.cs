using SS14.Client.Graphics.CluwneLib.Sprite;
using SS14.Shared.Maths;
using SS14.Client.Interfaces.Lighting;
using SS14.Client.Interfaces.Map;
using SS14.Client.Interfaces.Resource;
using SS14.Shared;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Color = SFML.Graphics.Color;


namespace SS14.Client.Services.Tiles
{
    public abstract class Tile : ITile, IQuadObject
    {
        protected readonly ILightManager _lightManager;
        protected readonly IResourceManager _resourceManager;
		protected CluwneSprite Sprite;
        protected Random _random;
        public List<TileDecal> decals;
        public Dictionary<GasType, int> gasAmounts;
		public CluwneSprite gasSprite;
        public string name;
        public DirectionFlags surroundDirsNW, surroundDirsSE = DirectionFlags.None;
        public TileState tileState = TileState.Healthy;
        public RectangleF bounds;
        public Direction _dir = Direction.North;

        protected Tile(TileState state, RectangleF rect)
        {
            _resourceManager = IoCManager.Resolve<IResourceManager>();
            _lightManager = IoCManager.Resolve<ILightManager>();
            
            tileState = state;

            bounds = rect;

            Sprite = _resourceManager.GetSprite("space_texture");
            Sprite.SetPosition(Position.X, Position.Y);
        }

        #region ITile Members

        
        public bool Visible { get; set; }

        public bool Opaque { get; set; } //Does this block LOS etc?
        public bool ConnectSprite { get; set; }
        //Should this tile cause things like walls to change their sprite to 'connect' to this tile?

        public Vector2 Position
        {
            get { return new Vector2(bounds.X, bounds.Y); }
        }

        public virtual void Render(float xTopLeft, float yTopLeft, Batch batch)
        {
            Sprite.Color = SFML.Graphics.Color.White;
            Sprite.SetPosition((float)Position.X - xTopLeft,
                               (float)Position.Y - yTopLeft);
            batch.AddClone(Sprite);
        }


        public virtual void RenderPos(float x, float y, int tileSpacing, int lightSize)
        {
            Sprite.Color = SFML.Graphics.Color.Transparent;
            Sprite.SetPosition(x, y);
            Sprite.Draw();
        }

        public virtual void RenderPosOffset(float x, float y, int tileSpacing, Vector2 lightPosition)
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
                            gasSprite.Color = new Color(255,5,0); 
                            break;
                        case GasType.WVapor:
                            gasSprite.Color = new Color(255 , 5 , 0); 
                            break;
                    }
                    gasBatch.AddClone(gasSprite);
                }
            }
        }

        public virtual void RenderTop(float xTopLeft, float yTopLeft, Batch wallTopsBatch)
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

        public virtual void SetSprite()
        {
        }

        public virtual void Initialize()
        {
            gasSprite = _resourceManager.GetSprite("gas");
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
                                             new Vector2(_random.Next(0, 64), _random.Next(0, 64)), this,
                                             new Color(165, 6, 6)));
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
            get { return bounds; }
        }
    }

    public class TileDecal
    {
        public Vector2 position; // Position relative to top left corner of tile
		public CluwneSprite sprite;
        public Tile tile;

		public TileDecal(CluwneSprite _sprite, Vector2 _position, Tile _tile, Color color)
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