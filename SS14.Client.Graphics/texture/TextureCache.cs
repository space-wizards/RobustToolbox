using SFMLTexture = SFML.Graphics.Texture;
using System.Collections.Generic;
using System;

namespace SS14.Client.Graphics.texture
{
    public static class TextureCache
    {
        private static Dictionary<string, Tuple<SFMLTexture, bool[,]>> _textures = null;

        public static Dictionary<string, Tuple<SFMLTexture, bool[,]>> Textures
        {
            get { return _textures; }
        }
        static TextureCache()
        {
            _textures = new Dictionary<string, Tuple<SFMLTexture, bool[,]>>();
        }
        public static bool Add(string name, SFMLTexture image, bool[,] arr)
        {
            if (_textures.ContainsKey(name))
                return true;

            _textures.Add(name, new Tuple<SFMLTexture, bool[,]>(new SFMLTexture(image), arr));

            return true;
        }
    }
}

