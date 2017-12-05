using SS14.Client.Graphics.Textures;
using System.Collections.Generic;

namespace SS14.Client.Graphics.TexHelpers
{
    public class TextureInfo
    {
        public Texture Texture { get; set; }
        public Image Image { get; set; }

        public TextureInfo(Texture tex, Image img)
        {
            Texture = tex;
            Image = img;
        }
    }

    public static class TextureCache
    {
        private static Dictionary<string, TextureInfo> _textures;

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

