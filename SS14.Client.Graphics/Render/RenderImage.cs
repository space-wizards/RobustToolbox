using SFML.Graphics;
using SFML.System;
using System;
using System.Diagnostics;

namespace SS14.Client.Graphics.Render
{
    /// <summary>
    /// Target for off-screen 2D rendering into a texture.
    /// </summary>
    [DebuggerDisplay("[RenderImage] Key = {Key} | X: {X} Y: {Y} W: {Width} H: {Height} SX: {Scale.X} SY: {Scale.Y} | IsDrawing = {DrawingToThis} |  BlitterSizeMode = {Mode} | Temp: {temp}" )]
    public class RenderImage : RenderTexture
    {

        // Crop or Scale
        private string _key;                // ID, Name of current instance
        private SFML.Graphics.Sprite blitsprite;          // Sprite used to blit
        private RenderTarget temp;          // Previous rendertarget
        private bool DrawingToThis = false;


        /// <summary>
        /// BlendMode.Alpha == Alpha Blending (default)
        ///
        ///
        /// Custom BlendMode
        /// SourceBlend           == Color Source Factor
        /// DestinationBlend      == Color Destination Factor
        /// SourceBlendAlpha      == Alpha Source Factor
        /// DestinationBlendAlpha == Alpha Destionation Factor
        ///
        /// SourceAlpha           == SrcAlpha
        /// InverseSourceAlpha    == OneMinusSrcAlpha
        /// </summary>
        public BlendMode BlendSettings = BlendMode.Alpha;

        #region Accessors

        public string Key
        {
            get {return _key;}
            set {_key = value;}
        }

        public float X
        {
            get
            {
                if(blitsprite != null)
                    return blitsprite.Position.X;
                return 0;
            }

        }

        public float Y
        {
            get
            {
                if(blitsprite != null)
                    return blitsprite.Position.Y;
                return 0;
            }

        }

        public uint Width
        {
            get { return Size.X; }
        }

        public uint Height
        {
            get { return Size.Y; }
        }

        new public Texture Texture
        {
            get { return base.Texture; }
        }

        public IntRect Crop
        {
            get;
            set;
        }

        public Vector2f Scale
        {
            get;
            set;
        }

        public bool UseDepthBuffer
        {
            get;
            set;
        }

        public BlitterSizeMode Mode
        {
            get;
            set;
        }

        #endregion

        #region Constructors
        /// <summary>
        /// Constructs a new RenderImage that can be rendered to
        /// </summary>
        /// <param name="key"> ID/key Of Instance</param>
        /// <param name="width"> Width of RenderImage </param>
        /// <param name="height"> Height of RenderImage </param>
        public RenderImage(string key, uint width, uint height): base(width, height)
        {
            CheckIfKeyIsNull(key);
            this._key = key;
        }

        public RenderImage(string key, int width, int height): this(key, (uint)width, (uint)height)
        {}

        /// <summary>
        /// Constructs a new RenderImage that can be rendered to
        /// </summary>
        /// <param name="key"> ID/key Of Instance</param>
        /// <param name="width"> Width of RenderImage </param>
        /// <param name="height"> Height of RenderImage </param>
        /// <param name="depthBuffer"> True to use a depthbuffer, false to exclude </param>
        public RenderImage(string key, uint width, uint height, bool depthBuffer) : base(width, height, depthBuffer)
        {
            CheckIfKeyIsNull(key);
            _key = key;
        }

        /// <summary>
        /// Constructs a new RenderImage that can be rendered to
        /// </summary>
        /// <param name="key"> ID/key Of Instance</param>
        /// <param name="width"> Width of RenderImage </param>
        /// <param name="height"> Height of RenderImage </param>
        /// <param name="depthBuffer"> True to use a depthbuffer, false to exclude </param>
        public RenderImage(string key, int width, int height, bool depthBuffer) : base((uint)width, (uint)height, depthBuffer)
        {
            CheckIfKeyIsNull(key);
            _key = key;
        }

        /// <summary>
        /// Constructs a new RenderImage that can be rendered to
        /// </summary>
        /// <param name="key"> Idenfication of RenderImage </param>
        /// <param name="width"> Width of RenderImage </param>
        /// <param name="height"> Height of RenderImage </param>
        /// <param name="imageBufferFormats"> Image Buffer Format to use </param>
        public RenderImage(string Key, int width, int height, ImageBufferFormats ibf): base((uint)width, (uint)height)
        {
            CheckIfKeyIsNull(Key);
            _key = Key;
        }

        /// <summary>
        /// Constructs a new RenderImage that can be rendered to
        /// </summary>
        /// <param name="key"> Idenfication of RenderImage </param>
        /// <param name="width"> Width of RenderImage </param>
        /// <param name="height"> Height of RenderImage </param>
        /// <param name="imageBufferFormats"> Image Buffer Format to use </param>
        public RenderImage(string Key, uint width, uint height, ImageBufferFormats IBF) : base(width, height)
        {
            CheckIfKeyIsNull(Key);
            _key = Key;
            BlendSettings = BlendMode.Alpha;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Enforce no null keys 4 ez debug
        /// </summary>
        /// <param name="key"></param>
        private void CheckIfKeyIsNull(string key)
        {
            if (key.Equals(null))
            {
                throw new Exception("key Cannot be null!");
            }
        }

        private void isStillDrawing()
        {
            if (DrawingToThis)
            {
                throw new Exception("Still Drawing to " + this._key );
            }
        }

        private void CheckDepthBuffer()
        {
            if (UseDepthBuffer)
            {

            }
        }

        /// <summary>
        /// Set the Current Render Target to this RenderImage
        /// </summary>
        public void BeginDrawing()
        {
            DrawingToThis = true;
            temp = CluwneLib.CurrentRenderTarget;
            CluwneLib.CurrentRenderTarget = this;
        }

        /// <summary>
        /// Reset the Render Target back to the previous
        /// </summary>
        public void EndDrawing()
        {
            DrawingToThis = false;
            base.Display();
            CluwneLib.CurrentRenderTarget = temp;
        }


        //Resets the rendertarget back to the screen
        public void ResetCurrentRenderTarget()
        {
            CluwneLib.ResetRenderTarget();
        }

        #endregion

        #region Drawing Methods

        /// <summary>
        /// Draws the RenderImage Texture to the screen
        /// </summary>
        /// <param name="posX"> Position X of Texture </param>
        /// <param name="posY"> Position Y of Texture </param>
        /// <param name="widthX"> Width of Texture </param>
        /// <param name="heightY"> Height of Texture </param>
        /// <param name="color"> Color of Texture</param>
        /// <param name="state"> Crop Or Scale </param>
        public void Blit(int posX, int posY, uint width, uint height, Color color, BlitterSizeMode state)
        {
            Mode = state;
            Blit(new Vector2f(posX, posY), new Vector2f(width, height), color);
        }

        /// <summary>
        /// Draws the RenderImage Texture to the screen
        /// </summary>
        /// <param name="posX"> Position X of Texture </param>
        /// <param name="posY"> Position Y of Texture </param>
        /// <param name="widthX"> Width of Texture </param>
        /// <param name="heightY"> Height of Texture </param>
        /// <param name="color"> Color of Texture </param>
        /// <param name="state"> Crop Or Scale </param>
        public void Blit(float posX, float posY, uint width, uint height, Color color, BlitterSizeMode state)
        {
            Mode = state;
            Blit(new Vector2f(posX, posY), new Vector2f(width, height), color);
        }

        /// <summary>
        /// Draws the RenderImage Texture to the screen
        /// </summary>
        /// <param name="posX"> Position X of Texture </param>
        /// <param name="posY"> Position Y of Texture </param>
        /// <param name="widthX"> Width of Texture </param>
        /// <param name="heightY"> Height of Texture </param>
        /// <param name="mode"> Crop or scale </param>
        public void Blit(int posX, int posY, uint widthX, uint heightY, BlitterSizeMode mode)
        {
            Mode = mode;
            Blit(new Vector2f(posX, posY), new Vector2f(widthX, heightY), Color.White);
        }

        /// <summary>
        /// Draws the RenderImage Texture to the screen
        /// </summary>
        /// <param name="posX"> Position X of CluwneSprite</param>
        /// <param name="posY"> Position Y of CluwneSprite </param>
        /// <param name="widthX"> Width of CluwneSprite </param>
        /// <param name="heightY"> Height of CluwneSprite </param>
        /// <param name="mode"> Crop or scale </param>
        public void Blit(float posX, float posY, uint widthX, uint heightY, BlitterSizeMode mode)
        {
            Mode = mode;
            Blit(new Vector2f(posX, posY), new Vector2f(widthX, heightY), Color.White);
        }



        /// <summary>
        /// Draws the RenderImage Texture to the screen
        /// </summary>
        /// <param name="posX"> Position X of Texture </param>
        /// <param name="posY"> Position Y of Texture </param>
        /// <param name="widthX"> Width of Texture </param>
        /// <param name="heightY"> Height of Texture </param>
        public void Blit(int posX, int posY, uint widthX, uint heightY)
        {
            Blit(new Vector2f(posX, posY), new Vector2f(widthX, heightY), Color.White);
        }

        /// <summary>
        /// Draws the RenderImage Texture to the screen
        /// </summary>
        /// <param name="posX"> Position X of CluwneSprite</param>
        /// <param name="posY"> Position Y of CluwneSprite </param>
        /// <param name="widthX"> Width of CluwneSprite </param>
        /// <param name="heightY"> Height of CluwneSprite </param>
        public void Blit(float posX, float posY, uint widthX, uint heightY)
        {
            Blit(new SFML.System.Vector2f(posX, posY), new SFML.System.Vector2f(widthX, heightY), Color.White);
        }


        public void Blit(RenderImage target)
        {
            Blit(new SFML.System.Vector2f(), new SFML.System.Vector2f(target.Size.X, target.Size.Y), Color.White);
        }



        /// <summary>
        /// Creates a Sprite from RenderImage Texture and draws it to the screen
        /// </summary>
        /// <param name="Position"> Position of Texture </param>
        /// <param name="Size"> Size of the Texture </param>
        /// <param name="color"> Global color of object </param>
        public void Blit(SFML.System.Vector2f position, SFML.System.Vector2f Size, SFML.Graphics.Color color)
        {
            isStillDrawing();
            blitsprite = new SFML.Graphics.Sprite(Texture);
            blitsprite.Position = position;
            blitsprite.Color = color;
            var bounds = blitsprite.GetLocalBounds();

            if (Mode == BlitterSizeMode.Scale)
            {
                SFML.System.Vector2f scale = new SFML.System.Vector2f(( Size.X / bounds.Width ),( Size.Y / bounds.Height ));
                blitsprite.Scale = scale;


            }
            else if (Mode == BlitterSizeMode.Crop)
            {
                IntRect crop = new IntRect((int)position.X, (int)position.Y, (int)Size.X, (int)Size.Y);
                blitsprite.TextureRect = crop;

            }


            if (CluwneLib.CurrentRenderTarget == this)
                return;

            blitsprite.Draw();


        }

        /// <summary>
        /// Scale & optionally crop
        /// </summary>
        /// <param name="destination">The exact rectangle you wish to fill - scales to this size</param>
        /// <param name="optionalcrop">To take a subset of the original texture instead of the whole texture</param>
        /// <param name="color">Color.</param>
        // public void Blit(IntRect destination, IntRect optionalcrop, Color color)
        // {
        //    CluwneSprite _blit;
        //    Display();
        //    if (optionalcrop == null)
        //        _blit = new CluwneSprite("_blit" + key, this);
        //    else
        //        _blit = new CluwneSprite("_blit" + key, base.Texture, optionalcrop);
        //    _blit.Color = color.ToSFMLColor();
        //    _blit.Draw(destination);
        // }

        #endregion


    }
}
