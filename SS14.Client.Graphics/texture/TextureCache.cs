using SS14.Client.Graphics.Collection;
using SFMLTexture = SFML.Graphics.Texture;

namespace SS14.Client.Graphics.texture
{
    public class TextureList : BaseCollection<SFMLTexture>
    {
        public SFMLTexture this[int index]
        {
            get { return GetItem(index); }
        }

        public SFMLTexture this[string key]
        {
            get { return GetItem(key); }
        }
        public void Add(string name, SFMLTexture tex)
        {
            AddItem(name, tex);
        }
        internal TextureList() : base(16, false) {}
    }
    public static class TextureCache
    {
        private static TextureList _textures = null;

        public static TextureList Textures
        {
            get { return _textures; }
        }
        static TextureCache()
        {
            _textures = new TextureList();
        }
        public static bool Add(string name, SFMLTexture image)
        {
            if (_textures.Contains(name))
                return true;

            _textures.Add(name, new SFMLTexture(image));

            return true;
        }
    }
}

