using OpenTK;
using SFML.Graphics;
using SFML.System;

namespace SS14.Client.Lighting
{
    public class QuadRenderer
    {
        private VertexArray vertex;

        public void LoadContent()
        {
            vertex = new VertexArray(PrimitiveType.Lines, 4);
            vertex[0] = new Vertex(new Vector2f( 1,-1),   Color.Transparent, new Vector2f(1, 1));
            vertex[1] = new Vertex(new Vector2f(-1,-1), Color.Transparent, new Vector2f(0, 1));
            vertex[2] = new Vertex(new Vector2f(-1, 1),  Color.Transparent, new Vector2f(0, 0));
            vertex[3] = new Vertex(new Vector2f( 1, 1),   Color.Transparent, new Vector2f(1, 0));



        }

        public void Render(Vector2 v1, Vector2 v2)
        {
            // TODO: Either add something here or remove this
            // What is the purpose of Quadrenderer? all it is doing is drawing transparent verts.
        }
    }
}
