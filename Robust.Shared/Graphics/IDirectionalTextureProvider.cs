using Robust.Shared.Maths;

namespace Robust.Shared.Graphics
{
    public interface IDirectionalTextureProvider
    {
        Texture Default { get; }
        Texture TextureFor(Direction dir);
    }
}
