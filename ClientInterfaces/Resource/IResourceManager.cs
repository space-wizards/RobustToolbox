using GorgonLibrary.Graphics;
using GorgonLibrary.Sprites;

namespace ClientInterfaces.Resource
{
    public interface IResourceManager
    {
        void LoadResourceZip();
        void ClearLists();
        Sprite GetSpriteFromImage(string key);
        Sprite GetSprite(string key);
        bool SpriteExists(string key);
        bool ImageExists(string key);
        FXShader GetShader(string key);
        Image GetImage(string key);
        Font GetFont(string key);
        SpriteInfo? GetSpriteInfo(string key);
    }
}
