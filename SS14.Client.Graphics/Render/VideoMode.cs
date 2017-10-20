using SVideoMove = SFML.Window.VideoMode;
using System.Linq;

namespace SS14.Client.Graphics.Render
{
    public struct VideoMode
    {
        internal SVideoMove SFMLVideoMode;

        public uint Height
        {
            get => SFMLVideoMode.Height;
            set => SFMLVideoMode.Height = value;
        }

        public uint Width
        {
            get => SFMLVideoMode.Width;
            set => SFMLVideoMode.Width = value;
        }

        public uint BitsPerPixel
        {
            get => SFMLVideoMode.BitsPerPixel;
            set => SFMLVideoMode.BitsPerPixel = value;
        }

        public VideoMode(uint width, uint height, uint bpp=32)
        {
            SFMLVideoMode = new SVideoMove(width, height, bpp);
        }

        public VideoMode(SVideoMove mode)
        {
            SFMLVideoMode = mode;
        }

        public bool Valid => SFMLVideoMode.IsValid();

        public static VideoMode[] FullscreenModes => SVideoMove.FullscreenModes.Select(m => new VideoMode(m)).ToArray();
        public static VideoMode DesktopMode => new VideoMode(SVideoMove.DesktopMode);
    }
}
