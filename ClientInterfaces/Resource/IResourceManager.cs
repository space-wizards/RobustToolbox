using GorgonLibrary.Graphics;
using GorgonLibrary.Sprites;
using GameObject;

namespace ClientInterfaces.Resource
{
    public interface IResourceManager
    {
        void LoadResourceZip(string path = null, string pw = null);
        void ClearLists();
        Sprite GetSpriteFromImage(string key);
        Sprite GetSprite(string key);
        bool SpriteExists(string key);
        bool ImageExists(string key);
        FXShader GetShader(string key);
        ParticleSettings GetParticles(string key);
        Image GetImage(string key);
        Font GetFont(string key);
        SpriteInfo? GetSpriteInfo(string key);
        object GetAnimatedSprite(string key);
        void LoadLocalResources();
        void LoadBaseResources();
    }
}