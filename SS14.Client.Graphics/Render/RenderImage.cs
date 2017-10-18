using SFML.Graphics;
using SFML.System;
using SS14.Client.Graphics.Textures;
using System;
using System.Diagnostics;
using SS14.Client.Graphics.Utility;
using SS14.Shared.Maths;
using Color = SS14.Shared.Maths.Color;
using Vector2u = SS14.Shared.Maths.Vector2u;
using Vector2i = SS14.Shared.Maths.Vector2i;
using Texture = SS14.Client.Graphics.Textures.Texture;
using Sprite = SS14.Client.Graphics.Sprites.Sprite;

namespace SS14.Client.Graphics.Render
{
    /// <summary>
    /// Target for off-screen 2D rendering into a texture.
    /// </summary>
    [DebuggerDisplay("[RenderImage] Key = {Key} | X: {X} Y: {Y} W: {Width} H: {Height} SX: {Scale.X} SY: {Scale.Y} | IsDrawing = {DrawingToThis} |  BlitterSizeMode = {Mode} | Temp: {temp}")]
    public class RenderImage : IRenderTarget, IDisposable
    {
        private RenderTexture _renderTexture;
        // Crop or Scale
        private Sprite blitsprite;          // Sprite used to blit
        private IRenderTarget temp;          // Previous rendertarget
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

        // ID, Name of current instance
        public string Key
        {
            get;
            set;
        }

        public float X
        {
            get
            {
                if (blitsprite != null)
                    return blitsprite.Position.X;
                return 0;
            }
        }

        public float Y
        {
            get
            {
                if (blitsprite != null)
                    return blitsprite.Position.Y;
                return 0;
            }
        }

        public Vector2u Size => _renderTexture.Size.Convert();
        public uint Width => _renderTexture.Size.X;
        public uint Height => _renderTexture.Size.Y;

        public Texture Texture => new Texture(_renderTexture.Texture);

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

        public RenderTarget SFMLTarget => _renderTexture;

        #endregion Accessors

        #region Constructors
        /// <summary>
        /// Constructs a new RenderImage that can be rendered to
        /// </summary>
        /// <param name="key"> ID/key Of Instance</param>
        /// <param name="width"> Width of RenderImage </param>
        /// <param name="height"> Height of RenderImage </param>
        public RenderImage(string key, uint width, uint height)
        {
            _renderTexture = new RenderTexture(width, height);
            CheckIfKeyIsNull(key);
            Key = key;
        }

        public RenderImage(string key, int width, int height) : this(key, (uint)width, (uint)height)
        { }

        /// <summary>
        /// Constructs a new RenderImage that can be rendered to
        /// </summary>
        /// <param name="key"> ID/key Of Instance</param>
        /// <param name="width"> Width of RenderImage </param>
        /// <param name="height"> Height of RenderImage </param>
        /// <param name="depthBuffer"> True to use a depthbuffer, false to exclude </param>
        public RenderImage(string key, uint width, uint height, bool depthBuffer)
        {
            _renderTexture = new RenderTexture(width, height, depthBuffer);
            CheckIfKeyIsNull(key);
            Key = key;
        }

        /// <summary>
        /// Constructs a new RenderImage that can be rendered to
        /// </summary>
        /// <param name="key"> ID/key Of Instance</param>
        /// <param name="width"> Width of RenderImage </param>
        /// <param name="height"> Height of RenderImage </param>
        /// <param name="depthBuffer"> True to use a depthbuffer, false to exclude </param>
        public RenderImage(string key, int width, int height, bool depthBuffer) : this(key, (uint)width, (uint)height, depthBuffer)
        {
        }

        /// <summary>
        /// Constructs a new RenderImage that can be rendered to
        /// </summary>
        /// <param name="key"> Idenfication of RenderImage </param>
        /// <param name="width"> Width of RenderImage </param>
        /// <param name="height"> Height of RenderImage </param>
        /// <param name="imageBufferFormats"> Image Buffer Format to use </param>
        public RenderImage(string Key, int width, int height, ImageBufferFormats ibf) : this(Key, (uint)width, (uint)height, ibf)
        {
        }

        /// <summary>
        /// Constructs a new RenderImage that can be rendered to
        /// </summary>
        /// <param name="key"> Idenfication of RenderImage </param>
        /// <param name="width"> Width of RenderImage </param>
        /// <param name="height"> Height of RenderImage </param>
        /// <param name="imageBufferFormats"> Image Buffer Format to use </param>
        public RenderImage(string key, uint width, uint height, ImageBufferFormats IBF)
        {
            _renderTexture = new RenderTexture(width, height);
            CheckIfKeyIsNull(Key);
            Key = Key;
            BlendSettings = BlendMode.Alpha;
        }

        #endregion Constructors

        public void Dispose()
        {
            _renderTexture.Dispose();
        }

        #region Helper Methods

        /// <summary>
        /// Enforce no null keys 4 ez debug
        /// </summary>
        /// <param name="key"></param>
        private void CheckIfKeyIsNull(string key)
        {
            // string.Equals(null) always returns false, so don't use it here
            if (key == null)
            {
                throw new Exception("key Cannot be null!");
            }
        }

        private void isStillDrawing()
        {
            if (DrawingToThis)
            {
                throw new Exception("Still Drawing to " + Key);
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
            _renderTexture.Display();
            CluwneLib.CurrentRenderTarget = temp;
        }

        //Resets the rendertarget back to the screen
        public void ResetCurrentRenderTarget()
        {
            CluwneLib.ResetRenderTarget();
        }

        #endregion Helper Methods

        #region Drawing Methods

        public void Draw(Drawable drawable)
        {
            _renderTexture.Draw(drawable);
        }

        public void Draw(IDrawable drawable)
        {
            Draw(drawable.SFMLDrawable);
        }

        public void Clear(Color color)
        {
            _renderTexture.Clear(color.Convert());
        }

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
            Blit(new Vector2(posX, posY), new Vector2(width, height), color);
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
            Blit(new Vector2(posX, posY), new Vector2(width, height), color);
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
            Blit(new Vector2(posX, posY), new Vector2(widthX, heightY), Color.White);
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
            Blit(new Vector2(posX, posY), new Vector2(widthX, heightY), Color.White);
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
            Blit(new Vector2(posX, posY), new Vector2(widthX, heightY), Color.White);
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
            Blit(new Vector2(posX, posY), new Vector2(widthX, heightY), Color.White);
        }

        public void Blit(RenderImage target)
        {
            Blit(Vector2.Zero, target.Size, Color.White);
        }

        /// <summary>
        /// Creates a Sprite from RenderImage Texture and draws it to the screen
        /// </summary>
        /// <param name="Position"> Position of Texture </param>
        /// <param name="size"> Size of the Texture </param>
        /// <param name="color"> Global color of object </param>
        public void Blit(Vector2 position, Vector2 size, Color color)
        {
            isStillDrawing();
            blitsprite = new Sprite(Texture);
            blitsprite.Position = position;
            blitsprite.Color = color;
            var bounds = blitsprite.LocalBounds;

            if (Mode == BlitterSizeMode.Scale)
            {
                var scale = new Vector2((size.X / bounds.Width), (size.Y / bounds.Height));
                blitsprite.Scale = scale;
            }
            else if (Mode == BlitterSizeMode.Crop)
            {
                Box2i crop = Box2i.FromDimensions((Vector2i)position, (Vector2i)size);
                blitsprite.TextureRect = crop;
            }

            if (CluwneLib.CurrentRenderTarget == this)
            {
                return;
            }

            blitsprite.Draw();
        }

        #endregion Drawing Methods
    }
}
