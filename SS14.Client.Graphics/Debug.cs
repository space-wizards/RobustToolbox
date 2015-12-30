using SFML.Graphics;
using SS14.Client.Graphics.Render;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Client.Graphics
{
    public class Debug
    {
        public bool DebugTextboxes { get; private set; }
        public bool DebugWalls     { get; private set; }
        public bool DebugAtmos     { get; private set; }
        public bool DebugEntities  { get; private set; }
        public bool DebugSprite    { get; private set; }
        public bool DebugColliders { get; private set; }

        public Debug()
        {
            DebugAtmos = false;
            DebugEntities = false;
            DebugTextboxes = false;
            DebugWalls = false;
        }

        public void ToggleAtmosDebug()
        {
            DebugAtmos = true;
        }

        public void ToggleWallDebug()
        {
            DebugWalls = true;
        }

        public void ToggleTextboxDebug()
        {
            DebugTextboxes = true;
        }

        public void ToggleEntitiesDebug()
        {
            DebugEntities= true;
        }

        public static void DebugRendertarget(RenderImage Rendertarget)
        {
            DebugRendertarget(Rendertarget, Rendertarget.Key);

        }

        public static void DebugRendertarget(RenderImage Rendertarget, string fileName)
        {
            //string path = "..\\DEBUGTEXTURE\\" + fileName + ".png";

            //Rendertarget.Texture.CopyToImage().SaveToFile(path);
        }



    }
}
