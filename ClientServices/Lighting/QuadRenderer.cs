using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GorgonLibrary;
using GorgonLibrary.Graphics;

namespace SS3D.LightTest
{
    public partial class QuadRenderer
    {
        VertexTypeList.PositionDiffuse2DTexture1[] verts = null;

        public QuadRenderer()
        {
        }

        public void LoadContent()
        {
            verts = new VertexTypeList.PositionDiffuse2DTexture1[4]
            {
                new VertexTypeList.PositionDiffuse2DTexture1( new Vector3D(1,-1,0), System.Drawing.Color.Transparent, new Vector2D(1,1)),
                new VertexTypeList.PositionDiffuse2DTexture1( new Vector3D(-1,-1,0), System.Drawing.Color.Transparent, new Vector2D(0,1)),
                new VertexTypeList.PositionDiffuse2DTexture1( new Vector3D(-1,1,0), System.Drawing.Color.Transparent, new Vector2D(0,0)),
                new VertexTypeList.PositionDiffuse2DTexture1( new Vector3D(1,1,0), System.Drawing.Color.Transparent, new Vector2D(1,0))
            };
        }

        public void Render(Vector2D v1, Vector2D v2)
        {
            // Ok this is dumb. You have to draw a non existant filledrectangle so the verts will draw, as drawing
            // one of these makes gorgon accept a TriangleList when you use the Draw() method, otherwise it will
            // want a pointlist which is no good to us.
            Gorgon.CurrentRenderTarget.FilledRectangle(0, 0, 0, 0, System.Drawing.Color.Black);

            Gorgon.CurrentRenderTarget.Draw(verts);
            Gorgon.CurrentRenderTarget.Update();
        }
    }
}
