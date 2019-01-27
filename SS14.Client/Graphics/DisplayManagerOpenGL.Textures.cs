using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using OpenTK.Graphics.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SS14.Shared.Log;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using ObjectLabelIdentifier = OpenTK.Graphics.OpenGL4.ObjectLabelIdentifier;
using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;

namespace SS14.Client.Graphics
{
    internal partial class DisplayManagerOpenGL
    {
        private readonly Dictionary<int, LoadedTexture> _loadedTextures = new Dictionary<int, LoadedTexture>();
        private int _nextTextureId;

        public Texture LoadTextureFromPNGStream(Stream stream, string name=null)
        {
            DebugTools.Assert(_mainThread == Thread.CurrentThread);

            // We use System.Drawing instead of ImageSharp because the latter has PNG loading bugs on Mono.
            // Even though supposedly that issue was fixed with Mono 5.14... I'm on 5.16.
            using (var image = Image.Load(stream))
            {
                return LoadTextureFromImage(image, name);
            }
        }

        public Texture LoadTextureFromImage<T>(Image<T> image, string name=null) where T : struct, IPixel<T>
        {
            DebugTools.Assert(_mainThread == Thread.CurrentThread);

            if (typeof(T) != typeof(Rgba32))
            {
                throw new NotImplementedException("Cannot load images other than Rgba32");
            }

            var texture = new OGLHandle(GL.GenTexture());
            GL.BindTexture(TextureTarget.Texture2D, texture.Handle);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                (int) TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                (int) TextureMagFilter.Nearest);

            var span = ((Image<Rgba32>) (object) image).GetPixelSpan();
            unsafe
            {
                fixed (Rgba32* ptr = &MemoryMarshal.GetReference(span))
                {
                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, image.Width, image.Height, 0,
                        PixelFormat.Rgba, PixelType.UnsignedByte, (IntPtr) ptr);
                }
            }

            if (name != null)
            {
                _objectLabelMaybe(ObjectLabelIdentifier.Texture, texture, name);
            }

            var loaded = new LoadedTexture
            {
                OpenGLObject = texture,
                Width = image.Width,
                Height = image.Height,
                Name = name,
            };

            var id = ++_nextTextureId;
            _loadedTextures.Add(id, loaded);

            return new OpenGLTexture(id, image.Width, image.Height);
        }

        private class LoadedTexture
        {
            public OGLHandle OpenGLObject;
            public int Width;
            public int Height;
            public string Name;
            public Vector2i Size => new Vector2i(Width, Height);
        }
    }
}
