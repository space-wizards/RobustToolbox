using SFML.Graphics;
using SFML.System;
using SS14.Client.Graphics.Render;
using SS14.Client.Graphics.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BlendMode = SS14.Client.Graphics.Render.BlendMode;
using RenderStates = SS14.Client.Graphics.Render.RenderStates;
using SBlendMode = SFML.Graphics.BlendMode;
using SRenderStates = SFML.Graphics.RenderStates;
using Texture = SS14.Client.Graphics.Textures.Texture;

namespace SS14.Client.Graphics.Sprites
{
    /// <summary>
    /// Provides optimized drawing of sprites
    /// </summary>
    [DebuggerDisplay("[SpriteBatch] IsDrawing: {Drawing} | ")]
    public class SpriteBatch : IDrawable, IDisposable
    {
        // If you use a class in another assembly, and any of its interfaces are from an unreferenced assembly
        // The C# compiler effectively dies and refuses to treat the class as implementing *ANYTHING*.
        // So if SpriteBatch class implements Drawable, it can't be passed to methods expecting IDrawable outside Client.Graphics.
        // What. The. Fuck.
        // So here's a dummy so I don't have to refactor all this IDrawable shit, which'd include more boiler plate.
        // God damnit it's 1 AM and I've spent way too long on something with such a fucking awful error message.
        // Can we just make rustc's error messages standard for every compiler?
        private class SpriteBatchDrawableDummy : Drawable
        {
            public SpriteBatch Parent { get; }

            public SpriteBatchDrawableDummy(SpriteBatch parent)
            {
                Parent = parent;
            }

            public void Draw(RenderTarget target, SRenderStates states)
            {
                Parent.Draw(target, states);
            }
        }
        private QueueItem activeItem;
        private List<QueueItem> QueuedTextures = new List<QueueItem>();
        private Queue<QueueItem> RecycleQueue = new Queue<QueueItem>();
        private readonly uint Max;
        private int count;
        private bool Drawing;

        public int Count
        {
            get { return count; }
        }

        public BlendMode BlendingSettings { get; set; }

        public SpriteBatch(uint maxCapacity = 100000)
        {
            Max = maxCapacity * 4;
            BlendingSettings = new BlendMode(BlendMode.Factor.SrcAlpha, BlendMode.Factor.OneMinusDstAlpha, BlendMode.Equation.Add, BlendMode.Factor.SrcAlpha, BlendMode.Factor.OneMinusSrcAlpha, BlendMode.Equation.Add);
            drawableDummy = new SpriteBatchDrawableDummy(this);
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

        public void Draw(IEnumerable<Sprite> sprites)
        {
            foreach (var s in sprites)
            {
                Draw(s);
            }
        }

        public void Draw(Sprite S)
        {
            count++;
            Using(S.Texture);
            // Scale is the offset of the other vertices.
            var Scale = new Vector2f(S.Scale.X, S.Scale.Y);
            Scale.X *= S.TextureRect.Width;
            Scale.Y *= S.TextureRect.Height;
            float sin = 0, cos = 1;

            var rads = OpenTK.MathHelper.DegreesToRadians(S.Rotation);
            sin = (float)Math.Sin(rads);
            cos = (float)Math.Cos(rads);

            var pX = -S.Origin.X * S.Scale.X;
            var pY = -S.Origin.Y * S.Scale.Y;

            // Top left
            activeItem.Verticies.Append
                (
                 new Vertex(
                        new Vector2f(
                            pX * cos - pY * sin + S.Position.X,
                            pX * sin + pY * cos + S.Position.Y),
                            S.Color.Convert(),
                        new Vector2f(
                            S.TextureRect.Left,
                            S.TextureRect.Top)
                            )
               );

            // Top right
            pX += Scale.X;
            activeItem.Verticies.Append
                (
                new Vertex(
                        new Vector2f(
                            pX * cos - pY * sin + S.Position.X,
                            pX * sin + pY * cos + S.Position.Y),
                            S.Color.Convert(),
                        new Vector2f(
                            S.TextureRect.Right,
                            S.TextureRect.Top)
                          )
                );

            // Bottom right.
            pY += Scale.Y;
            activeItem.Verticies.Append
                (
                new Vertex(
                        new Vector2f(
                            pX * cos - pY * sin + S.Position.X,
                            pX * sin + pY * cos + S.Position.Y),
                            S.Color.Convert(),
                        new Vector2f(
                            S.TextureRect.Right,
                            S.TextureRect.Bottom)
                         )
                );

            pX -= Scale.X;

            // Bottom left.
            activeItem.Verticies.Append(
                new Vertex(
                        new Vector2f(
                            pX * cos - pY * sin + S.Position.X,
                            pX * sin + pY * cos + S.Position.Y),
                            S.Color.Convert(),
                        new Vector2f(
                            S.TextureRect.Left,
                            S.TextureRect.Bottom)
                        )
                );
        }

        public void Draw(RenderTarget target, SRenderStates Renderstates)
        {
            if (Drawing)
            {
                throw new InvalidOperationException("Call End first.");
            }

            foreach (var item in QueuedTextures)
            {
                Renderstates.Texture = item.Texture.SFMLTexture;
                Renderstates.BlendMode = (SBlendMode)BlendingSettings;

                item.Verticies.Draw(target, Renderstates);
            }
        }

        public void Draw()
        {
            Draw(CluwneLib.CurrentRenderTarget, CluwneLib.ShaderRenderState);
        }

        public void Draw(IRenderTarget target, RenderStates renderStates)
        {
            Draw(target.SFMLTarget, renderStates.SFMLRenderStates);
        }

        Drawable IDrawable.SFMLDrawable => drawableDummy;

        private SpriteBatchDrawableDummy drawableDummy;

        ~SpriteBatch()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var item in QueuedTextures.Union(RecycleQueue))
                {
                    item.Verticies.Dispose();
                }
                QueuedTextures.Clear();
                RecycleQueue.Clear();
            }
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
