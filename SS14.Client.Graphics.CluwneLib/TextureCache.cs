using SFML.Graphics;
using SS14.Client.Graphics.CluwneLib.Collection;


namespace SS14.Client.Graphics.CluwneLib
{
    public class TextureList
        : BaseCollection<Texture>
    {
        public Texture this[int index]
        {
            get { return GetItem(index); }
        }

        public Texture this[string key]
        {
            get { return GetItem(key); }
        }
        public void Add(string name, Texture tex)
        {
            AddItem(name, tex);
        }
        internal TextureList()
            : base(16, false) {}
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
        public static bool Add(string name, Image image) {
            if (_textures.Contains(name))
                return true;

            _textures.Add(name, new Texture(image));

            return true;
        }
    }
}

