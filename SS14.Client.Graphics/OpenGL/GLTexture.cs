//OpenGL v4 or before?
using OpenTK.Graphics.OpenGL;
using SFML.Graphics;
using SS14.Client.Graphics.Shader;
using System;
using PrimitiveType = OpenTK.Graphics.OpenGL.PrimitiveType;

namespace SS14.Client.Graphics.OpenGL
{
    public class GLTexture
    {
        private ImageBufferFormats FBOFormat;
        private PixelInternalFormat InternalFormat;
        private ErrorCode GLErrorCode;
        private bool DepthBuffer;
     
        private bool active;

        private uint texturep;

        public string Key
        {
            get;
            set;
        }
        public int Width
        {
            get;
            set;
        }
        public int Height
        {
            get;
            set;
        }
        


      
        public GLTexture(string key, int width, int height, ImageBufferFormats format)
        {
            FBOFormat = format;
            Key = key;
            Width = width;
            Height = height;

            Create();
          
        }

        public GLTexture(string key, int width, int height, bool depthbuffer, ImageBufferFormats format)
        {
            DepthBuffer = depthbuffer;
            FBOFormat = format;
            Key = key;
            Width = width;
            Height = height;

            Create();
        }

        private void CheckForErrors()
        {
            GLErrorCode = GL.GetError();

            if ( GLErrorCode != ErrorCode.NoError)
                Console.Out.Write(GLErrorCode);
        }

        private void CheckImageBufferFormat()
        {
            if (FBOFormat.Equals(ImageBufferFormats.BufferGR1616F))
            {
                InternalFormat = PixelInternalFormat.Rg16f;
            }
            else
            {
                InternalFormat = PixelInternalFormat.Rgba8;
            }
        }

        

        private void Create()
        {
            active = true;         

            // Configure the viewport (the same size as the window)
             GL.Viewport(0, 0, (int)CluwneLib.Screen.Size.X, (int)CluwneLib.Screen.Size.Y);   

            //create a 2D Texture 
             GL.Enable(EnableCap.Texture2D);
             GL.GenTextures(1, out texturep);
             GL.BindTexture(TextureTarget.Texture2D, texturep);

             CheckImageBufferFormat();
             GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat, Width, Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);

             GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
             GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);
             GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
             GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);


       

          
        }

        public void Begin()
        {
           active = true;

        }

 

        public void Clear(Color clearcolor)
        {            
            GL.ClearColor(clearcolor.R, clearcolor.G, clearcolor.B, clearcolor.A);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }

        public void Blit(Texture texture, GLSLShader shader)
        {
          
            //BindShader(shader);
            Texture.Bind(texture);
            GLSLShader.Bind(shader);

           
            //begin drawing
            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(0, 0); GL.Vertex2(0, 0);
            GL.TexCoord2(1, 0); GL.Vertex2(1, 0);
            GL.TexCoord2(1, 1); GL.Vertex2(1, -1); 
            GL.TexCoord2(0, 1); GL.Vertex2(0, -1); 
            GL.End();



            //debug
           // texture.CopyToImage().SaveToFile("..\\GLTexture.png");

     
            // Finally, display the rendered frame on screen
           CluwneLib.Screen.Display();//SWAPS BUFFER
           Texture.Bind(null);
           GLSLShader.Bind(null);
      
        }


        public void End()
        {
           active = false;     
        }

       


       
    }
}
