using SS14.Client.Graphics;
using SS14.Client.Graphics.VertexData;
using SS14.Shared.Maths;
using System.Drawing;

namespace SS14.Client.Services.Lighting
{
    public class QuadRenderer
    {
        private VertexTypeList.PositionDiffuse2DTexture1[] verts;

        public void LoadContent()
        {
            verts = new VertexTypeList.PositionDiffuse2DTexture1[4]
                        {
                            new VertexTypeList.PositionDiffuse2DTexture1(new Vector3(1, -1, 0), Color.Transparent,
                                                                         new Vector2(1, 1)),
                            new VertexTypeList.PositionDiffuse2DTexture1(new Vector3(-1, -1, 0), Color.Transparent,
                                                                         new Vector2(0, 1)),
                            new VertexTypeList.PositionDiffuse2DTexture1(new Vector3(-1, 1, 0), Color.Transparent,
                                                                         new Vector2(0, 0)),
                            new VertexTypeList.PositionDiffuse2DTexture1(new Vector3(1, 1, 0), Color.Transparent,
                                                                         new Vector2(1, 0))
                        };
        }

        public void Render(Vector2 v1, Vector2 v2)
        {
            // Ok this is dumb. You have to draw a non existant filledrectangle so the verts will draw, as drawing
            // one of these makes gorgon accept a TriangleList when you use the Draw() method, otherwise it will
            // want a pointlist which is no good to us.
           CluwneLib.drawRectangle(0, 0, 0, 0, Color.Black);
         
           
            
        }
    }
}