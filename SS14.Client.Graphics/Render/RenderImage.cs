using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SFML.Graphics;
using Color = System.Drawing.Color;
using SS14.Client.Graphics.Sprite;
using SS14.Shared.Maths;

namespace SS14.Client.Graphics.Render
{
    /// <summary>
    /// Creates RenderImages that can be rendered to
    /// </summary>
    public class RenderImage : RenderTexture
    {
      
        private ImageBufferFormats imageBufferFormats;
        private RenderTarget _temp;
        public IntRect Crop;
        public Vector2 Scale;
        private string _key;         // ID, Name of current instance
        
        /// <summary>
        /// Constructs a new RenderImage that can be rendered to 
        /// </summary>
        /// <param name="width"> Width of RenderImage </param>
        /// <param name="height"> Height of RenderImage </param>
        public RenderImage(uint width, uint height) : base(width, height)
        {
        }

        /// <summary>
        /// Constructs a new RenderImage that can be rendered to 
        /// </summary>
        /// <param name="width"> Width of RenderImage </param>
        /// <param name="height"> Height of RenderImage </param>
        /// <param name="depthBuffer"> True to use a depthbuffer, false to exclude </param>
        public RenderImage(uint width, uint height, bool depthBuffer) : base(width, height, depthBuffer)
        {
        }

        /// <summary>
        /// Constructs a new RenderImage that can be rendered to 
        /// </summary>
        /// <param name="width"> Width of RenderImage </param>
        /// <param name="height"> Height of RenderImage </param>
        /// <param name="imageBufferFormats"> Image Buffer Format to use </param>
        public RenderImage( uint width, uint height, ImageBufferFormats imageBufferFormats) : base(width,height)
        {
            this.imageBufferFormats = imageBufferFormats;
        }

        /// <summary>
        /// Constructs a new RenderImage that can be rendered to 
        /// </summary>
        /// <param name="Key"> Idenfication of RenderImage </param>
        /// <param name="width"> Width of RenderImage </param>
        /// <param name="height"> Height of RenderImage </param>
        /// <param name="imageBufferFormats"> Image Buffer Format to use </param>
        public RenderImage(string Key, int width, int height, ImageBufferFormats imageBufferFormats) : base((uint)width,(uint)height)
        {
            this._key = Key;
            this.imageBufferFormats = imageBufferFormats;
        }

        /// <summary>
        /// Constructs a new RenderImage that can be rendered to 
        /// </summary>
        /// <param name="Key"> Idenfication of RenderImage </param>
        /// <param name="width"> Width of RenderImage </param>
        /// <param name="height"> Height of RenderImage </param>
        /// <param name="imageBufferFormats"> Image Buffer Format to use </param>
        public RenderImage(string Key, uint width, uint height, ImageBufferFormats imageBufferFormats)  : base(width, height)
        {
            this.imageBufferFormats = imageBufferFormats;
        }

        /// <summary>
        /// Draws the RenderImage to a CluwneSprite and then to the current RenderTarget
        /// </summary>
        /// <param name="posX"> Position X of CluwneSprite</param>
        /// <param name="posY"> Position Y of CluwneSprite </param>
        /// <param name="widthX"> Width of CluwneSprite </param>
        /// <param name="heightY"> Height of CluwneSprite </param>
        /// <param name="color"> Color of CluwneSprite</param>
        /// <param name="state"> RenderState </param>
        public void Blit(int posX, int posY, uint width, uint height, Color color, BlitterSizeMode state) {
            throw new NotImplementedException("The API makes no sense, use a different blit overload.");
            // This interface is nonsensical for Crop, since Crop needs a rectangle and not two points.
            // the Crop+Scale overload is what you actually want to use.
        }

        /// <summary>
        /// Draws the entire Renderimage to the named position.
        /// </summary>
        public void Blit(Vector2 Position, Color color)
        {
            Display();
            CluwneSprite _blit = new CluwneSprite("_blit" + _key, base.Texture);
            _blit.Position=Position;
            _blit.Color = CluwneLib.SystemColorToSFML(color);
            _blit.Draw();
        }
        /// <summary>
        /// Scale & optionally crop
        /// </summary>
        /// <param name="destination">The exact rectangle you wish to fill - scales to this size</param>
        /// <param name="optionalcrop">To take a subset of the original texture instead of the whole texture</param>
        /// <param name="color">Color.</param>
        public void Blit(IntRect destination, IntRect optionalcrop, Color color) {
            CluwneSprite _blit;
            Display();
            if (optionalcrop == null)
                _blit=new CluwneSprite("_blit" + _key, base.Texture);
            else
                _blit=new CluwneSprite("_blit" + _key, base.Texture, optionalcrop);
            _blit.Color=CluwneLib.SystemColorToSFML(color);
            _blit.Draw(destination);
        }


        /// <summary>
        /// Draws the RenderImage to a CluwneSprite and then to the current RenderTarget
        /// </summary>
        /// <param name="posX"> Position X of CluwneSprite</param>
        /// <param name="posY"> Position Y of CluwneSprite </param>
        /// <param name="widthX"> Width of CluwneSprite </param>
        /// <param name="heightY"> Height of CluwneSprite </param>
        public void Blit(int posX, int posY, uint widthX, uint heightY)
        {
            this.Display();
            CluwneSprite _blit = new CluwneSprite("_blit" + _key, this);
            _blit.Position = new Vector2(posX, posY);
            _blit.Size = new Vector2(widthX, heightY);
            _blit.Color = CluwneLib.SystemColorToSFML(Color.Transparent);
            _blit.Draw();
        }

        public void BeginDrawing()
        {
			_temp=CluwneLib.CurrentRenderTarget;
			CluwneLib.CurrentRenderTarget = this;
        }

        public void EndDrawing()
        {
			if (_temp == null)
				return;
			CluwneLib.CurrentRenderTarget = _temp;
        }

        /// <summary>
        /// Clears the RenderImage with the specified color
        /// </summary>
        /// <param name="Color"> Color used to cover everything </param>
        public void Clear(Color Color)
        {
            this.Clear(CluwneLib.SystemColorToSFML(Color));
        }

        public uint Width  { get { return Size.X; } }
        public uint Height { get { return Size.Y; } }

        public string setName { get; set; }

        public ImageBufferFormats setImageBuffer { get; set; }

        ~RenderImage() {
            System.Console.WriteLine("Requesting SFML_Lock");
            CluwneLib.RequestGC(() => {
                base.Dispose();
            });
            System.Console.WriteLine("Render image destroyed");
        }
    }
}
