namespace Robust.Client.Graphics
{
    // TODO: Maybe implement IDisposable for owned textures. I got lazy and didn't.
    /// <summary>
    ///     Represents a mutable texture that can be modified and deleted.
    /// </summary>
    public abstract class OwnedTexture : Texture
    {
        public abstract void Delete();
    }
}
