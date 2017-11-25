using SS14.Client.Graphics.Textures;
using System.Collections.Generic;

namespace SS14.Client.Graphics.TexHelpers
{
    public class TextureInfo
    {
        private Texture texture;
        private Image image;

        public Texture Texture { get => texture; set => texture = value; }
        public Image Image { get => image; set => image = value; }

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

