using Robust.Shared.Maths;

namespace Robust.Client.Graphics
{
    public interface IDirectionalTextureProvider
    {
        Texture Default { get; }
        Texture TextureFor(Direction dir);
    }
}
