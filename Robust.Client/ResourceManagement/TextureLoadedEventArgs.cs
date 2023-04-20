using Robust.Shared.Utility;
using SixLabors.ImageSharp;

namespace Robust.Client.ResourceManagement
{
    public readonly struct TextureLoadedEventArgs
    {
        internal TextureLoadedEventArgs(ResPath path, Image image, TextureResource resource)
        {
            Path = path;
            Image = image;
            Resource = resource;
        }

        public ResPath Path { get; }
        public Image Image { get; }
        public TextureResource Resource { get; }
    }
}
