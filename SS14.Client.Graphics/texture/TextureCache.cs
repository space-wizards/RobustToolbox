using SFMLTexture = SFML.Graphics.Texture;
using SFML.Graphics;
using System.Collections.Generic;
using System;

namespace SS14.Client.Graphics.texture
{
    public static class TextureCache
    {
        private static Dictionary<string, Tuple<SFMLTexture, bool[,], Image>> _textures = null;

        public static Dictionary<string, Tuple<SFMLTexture, bool[,], Image>> Textures
        {
            get { return _textures; }
        }
        static TextureCache()
        {
            _textures = new Dictionary<string, Tuple<SFMLTexture, bool[,], Image>>();
        }
        public static bool Add(string name, SFMLTexture image, bool[,] arr, Image rimg)
        {
            if (_textures.ContainsKey(name))
                return true;

            _textures.Add(name, new Tuple<SFMLTexture, bool[,], Image>(new SFMLTexture(image), arr, rimg));

            return true;
        }
    }
}

