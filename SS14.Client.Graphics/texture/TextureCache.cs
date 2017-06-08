using SFMLTexture = SFML.Graphics.Texture;
using SFML.Graphics;
using System.Collections.Generic;

namespace SS14.Client.Graphics.TexHelpers
{
    public class TextureInfo
    {
        public SFMLTexture Texture;
        public Image Image;
        public bool[,] Opacity;

        public TextureInfo(SFMLTexture tex, Image img, bool[,] opacity)
        {
            Texture = tex;
            Image = img;
            Opacity = opacity;
        }
    }

    public static class TextureCache
    {
        private static Dictionary<string, TextureInfo> _textures = null;

        public static Dictionary<string, TextureInfo> Textures
        {
            get { return _textures; }
        }
        static TextureCache()
        {
            _textures = new Dictionary<string, TextureInfo>();
        }
        public static bool Add(string name, TextureInfo texinfo)
        {
            if (_textures.ContainsKey(name))
                return true;

            _textures.Add(name, texinfo);

            return true;
        }
    }
}

