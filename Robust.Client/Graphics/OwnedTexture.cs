using System;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics
{
    /// <summary>
    ///     Represents a mutable texture that can be modified and deleted.
    /// </summary>
    public abstract class OwnedTexture : Texture, IDisposable
    {
        protected OwnedTexture(Vector2i size) : base(size)
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [Obsolete("Use Dispose() instead")]
        public void Delete()
        {
            Dispose();
        }

        protected virtual void Dispose(bool disposing)
        {

        }

        ~OwnedTexture()
        {
            Dispose(false);
        }
    }
}
