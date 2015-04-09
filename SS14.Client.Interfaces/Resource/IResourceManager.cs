using SFML.Graphics;
using SS14.Client.Graphics.CluwneLib.Shader;
using SS14.Client.Graphics.CluwneLib.Sprite;
using SS14.Shared.GameObjects;

namespace SS14.Client.Interfaces.Resource
{
    public interface IResourceManager
    {
        void LoadResourceZip(string path = null, string pw = null);
        void ClearLists();
        CluwneSprite GetSpriteFromImage(string key);
        CluwneSprite GetSprite(string key);
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
        CluwneSprite GetNoSprite();
    }
}