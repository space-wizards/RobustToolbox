using SFML.Graphics;
using SFML.System;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SS14.Client.Graphics.Sprite
{
    /// <summary>
    /// Provides optimized drawing of sprites
    /// </summary>
    [DebuggerDisplay("[SpriteBatch] IsDrawing: {Drawing} | ")]
    public class SpriteBatch : Drawable
    {    

        private QueueItem activeItem;
        private List<QueueItem> QueuedTextures = new List<QueueItem>();
        private Queue<QueueItem> RecycleQueue = new Queue<QueueItem>();
        private readonly uint Max;
        private int count;
        private bool Drawing;
        private uint queueCount;
        
        public int Count
        {
            get { return count; }
        }

        public BlendMode BlendingSettings;
      
        public SpriteBatch(uint maxCapacity = 100000)
        {
            Max = maxCapacity * 4;
            BlendingSettings = new BlendMode(BlendMode.Factor.SrcAlpha, BlendMode.Factor.OneMinusDstAlpha, BlendMode.Equation.Add, BlendMode.Factor.SrcAlpha, BlendMode.Factor.OneMinusSrcAlpha, BlendMode.Equation.Add);
        }

        public void BeginDrawing()
        {          
            count = 0;
            // we use these a lot, and the overall number of textures
            // remains stable, so recycle them to avoid excess calls into
            // the native constructor.
            foreach (var Entry in QueuedTextures)
            {
                Entry.Verticies.Clear();
                Entry.Texture = null;
                RecycleQueue.Enqueue(Entry);
            }
            QueuedTextures.Clear();
            Drawing = true;
            activeItem = null;
        }

        public void EndDrawing()
        {            
            Drawing = false;
        }

        private void Using(Texture texture)
        {
            if (!Drawing) 
               throw new Exception("Call Begin first.");

            if (activeItem == null || activeItem.Texture != texture)
            {
                if (RecycleQueue.Count > 0)
                {
                    activeItem = RecycleQueue.Dequeue();
                    activeItem.Texture = texture;
                }
                else
                {
                   activeItem = new QueueItem(texture);
                }   
                QueuedTextures.Add(activeItem);
            }
        }

        public void Draw(IEnumerable<SFML.Graphics.Sprite> sprites)
        {
            foreach (var s in sprites)
                Draw(s);
        }

        public void Draw(SFML.Graphics.Sprite S)
        {
            count++;
            Using(S.Texture);
            Vector2f Scale = new Vector2f(S.Scale.X, S.Scale.Y);
            float sin = 0, cos = 1;
            //FloatMath.SinCos(rotation, out sin, out cos);

            S.Rotation = S.Rotation / 180 * (float)Math.PI;
            sin = (float)Math.Sin(S.Rotation);
            cos = (float)Math.Cos(S.Rotation);

            var pX = -S.Origin.X * S.Scale.X;
            var pY = -S.Origin.Y * S.Scale.Y;
            Scale.X *= S.TextureRect.Width;
            Scale.Y *= S.TextureRect.Height;

            activeItem.Verticies.Append
                (
                 new Vertex(
                        new SFML.System.Vector2f(
                            pX * cos - pY * sin + S.Position.X,
                            pX * sin + pY * cos + S.Position.Y),
                            S.Color,
                        new SFML.System.Vector2f(
                            S.TextureRect.Left,
                            S.TextureRect.Top)
                            )
               );


            pX += Scale.X;
            activeItem.Verticies.Append
                (
                new Vertex(
                        new SFML.System.Vector2f(   
                            pX * cos - pY * sin + S.Position.X,
                            pX * sin + pY * cos + S.Position.Y),
                            S.Color,
                        new SFML.System.Vector2f(
                            S.TextureRect.Left + S.TextureRect.Width,
                            S.TextureRect.Top)
                          )
                );

            pY += Scale.Y;
            activeItem.Verticies.Append
                (
                new Vertex(
                        new SFML.System.Vector2f(
                            pX * cos - pY * sin + S.Position.X,
                            pX * sin + pY * cos + S.Position.Y),
                            S.Color,
                        new SFML.System.Vector2f(
                            S.TextureRect.Left + S.TextureRect.Width,
                            S.TextureRect.Top + S.TextureRect.Height)
                         )
                );

            pX -= Scale.X;

            activeItem.Verticies.Append(
                new Vertex(
                        new SFML.System.Vector2f(
                            pX * cos - pY * sin + S.Position.X,
                            pX * sin + pY * cos + S.Position.Y),
                            S.Color,
                        new SFML.System.Vector2f(
                            S.TextureRect.Left,
                            S.TextureRect.Top + S.TextureRect.Height)
                        )
                );
        }

       
        /*
                public unsafe void Draw(Texture texture, FloatRect rec, IntRect src, Color color)
                {
                    var index = Create(texture);

                    fixed (Vertex* fptr = vertices)
                    {
                        var ptr = fptr + index;

                        ptr->Position.X = rec.Left;
                        ptr->Position.Y = rec.Top;
                        ptr->TexCoords.X = src.Left;
                        ptr->TexCoords.Y = src.Top;
                        ptr->Color = color;
                        ptr++;

                        ptr->Position.X = rec.Left + rec.Width;
                        ptr->Position.Y = rec.Top;
                        ptr->TexCoords.X = src.Left + src.Width;
                        ptr->TexCoords.Y = src.Top;
                        ptr->Color = color;
                        ptr++;

                        ptr->Position.X = rec.Left + rec.Width;
                        ptr->Position.Y = rec.Top + rec.Height;
                        ptr->TexCoords.X = src.Left + src.Width;
                        ptr->TexCoords.Y = src.Top + src.Height;
                        ptr->Color = color;
                        ptr++;

                        ptr->Position.X = rec.Left;
                        ptr->Position.Y = rec.Top + rec.Height;
                        ptr->TexCoords.X = src.Left;
                        ptr->TexCoords.Y = src.Top + src.Height;
                        ptr->Color = color;
                    }
                }

                public void Draw(Texture texture, FloatRect rec, Color color)
                {
                    int width = 1, height = 1;
                    if (texture != null)
                    {
                        width = (int)texture.Size.X;
                        height = (int)texture.Size.Y;
                    }
                    Draw(texture, rec, new IntRect(0, 0, width, height), color);
                }

                public void Draw(Texture texture, Vector2f pos, Color color)
                {
                    if (texture == null) throw new ArgumentNullException();
                    var width = (int)texture.Size.X;
                    var height = (int)texture.Size.Y;
                    Draw(texture, new FloatRect(pos.X, pos.Y, width, height), new IntRect(0, 0, width, height), color);
                }

        */
        public void Draw(RenderTarget target, RenderStates Renderstates)
        {
            if (Drawing) throw new Exception("Call End first.");


            foreach (var item in QueuedTextures)
            {
                Renderstates.Texture = item.Texture;
                Renderstates.BlendMode = BlendingSettings;

                item.Verticies.Draw(target, Renderstates);

            }
        }

        public void Dispose()
        {

        }

        [DebuggerDisplay("[QueueItem] Name: {ID} | Texture: {Texture} | Verticies: {Verticies}")]
        private class QueueItem
        {
            public Texture Texture;
            public VertexArray Verticies;

            public QueueItem(Texture Tex)
            {
                Texture = Tex;
                Verticies = new VertexArray(PrimitiveType.Quads);
            }
        }

    }
}
