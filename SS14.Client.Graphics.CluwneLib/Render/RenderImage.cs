using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SFML.Graphics;
using Color = System.Drawing.Color;
using SS14.Client.Graphics.CluwneLib.Sprite;
using SS14.Shared.Maths;


namespace SS14.Client.Graphics.CluwneLib.Render
{
    /// <summary>
    /// Creates RenderImages that can be rendered to
    /// </summary>
    public class RenderImage : RenderTexture
    {
      
        private ImageBufferFormats imageBufferFormats;
        private RenderTexture _temp;
        private Image _renderImage;    // Image used as a RenderTarget
        private CluwneSprite _blit;  // RenderImage drawn in a sprite
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
        public void Blit(int posX, int posY, uint widthX, uint heightY, Color color, BlitterSizeMode blitterSizeMode)
        {
           _blit = new CluwneSprite("_blit" + _key, this);
           _blit.Position = new Vector2(posX, posY);
           _blit.Size = new Vector2(widthX, heightY);
           _blit.Color = CluwneLib.SystemColorToSFML(color);

           if (blitterSizeMode == BlitterSizeMode.Crop)
           { 
               //crop image
           }

           if (blitterSizeMode == BlitterSizeMode.Scale)
           { 
                //scale image
           }


           _blit.Draw();

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
            _blit = new CluwneSprite("_blit" + _key, this);
            _blit.Position = new Vector2(posX, posY);
            _blit.Size = new Vector2(widthX, heightY);
            _blit.Color = CluwneLib.SystemColorToSFML(Color.Transparent);


            _blit.Draw();


        }

       
        public void BeginDrawing()
        {
            
        }

        public void EndDrawing()
        {

        }


        /// <summary>
        /// Clears the RenderImage with the specified color
        /// </summary>
        /// <param name="Color"> Color used to cover everything </param>
        public void Clear(Color Color)
        {
            this.Clear(CluwneLib.SystemColorToSFML(Color));
        }



        public uint Width { get; set; }
        public uint Height { get; set; }
        public string setName { get; set; }

        public ImageBufferFormats setImageBuffer { get; set; }
    }
}
