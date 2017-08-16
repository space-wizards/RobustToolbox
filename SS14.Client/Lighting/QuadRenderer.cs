using SFML.Graphics;
using SFML.System;

namespace SS14.Client.Lighting
{
    public class QuadRenderer
    {
       // private VertexTypeList.PositionDiffuse2DTexture1[] verts;
        private VertexArray vertex;

        public void LoadContent()
        {
            //verts = new VertexTypeList.PositionDiffuse2DTexture1[4]
            //            {
            //                new VertexTypeList.PositionDiffuse2DTexture1(new Vector3(1, -1, 0), Color.Transparent, new Vector2(1, 1)),
            //                new VertexTypeList.PositionDiffuse2DTexture1(new Vector3(-1, -1, 0), Color.Transparent,new Vector2(0, 1)),
            //                new VertexTypeList.PositionDiffuse2DTexture1(new Vector3(-1, 1, 0), Color.Transparent, new Vector2(0, 0)),
            //                new VertexTypeList.PositionDiffuse2DTexture1(new Vector3(1, 1, 0), Color.Transparent, new Vector2(1, 0))
            //            };

            vertex = new VertexArray(PrimitiveType.Lines, 4);
            vertex[0] = new Vertex(new Vector2( 1,-1),   Color.Transparent, new Vector2(1, 1));
            vertex[1] = new Vertex(new Vector2(-1,-1), Color.Transparent, new Vector2(0, 1));
            vertex[2] = new Vertex(new Vector2(-1, 1),  Color.Transparent, new Vector2(0, 0));
            vertex[3] = new Vertex(new Vector2( 1, 1),   Color.Transparent, new Vector2(1, 0));
            


        }

        public void Render(Vector2 v1, Vector2 v2)
        {
       


            // What is the purpose of Quadrenderer? all it is doing is drawing transparent verts.
           // CluwneLib.CurrentRenderTarget.Draw(vertex);
         
           
            
        }
    }
}