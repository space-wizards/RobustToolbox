using SFML.Graphics;
using SS14.Client.Graphics.Shader;
using SS14.Client.Graphics.Sprite;
using SS14.Shared.GameObjects;
using System.Collections.Generic;

namespace SS14.Client.Interfaces.Resource
{
    public interface IResourceManager
    {
        Dictionary<Texture, string> textureToKey { get; }
        void LoadResourceZip(string path = null, string pw = null);
        void ClearLists();
        Sprite GetSpriteFromImage(string key);
        Sprite GetSprite(string key);
        bool SpriteExists(string key);
        bool TextureExists(string key);
        GLSLShader GetShader(string key);
        TechniqueList GetTechnique(string key);
        ParticleSettings GetParticles(string key);
        Texture GetTexture(string key);
        Font GetFont(string key);
        SpriteInfo? GetSpriteInfo(string key);
        object GetAnimatedSprite(string key);
        void LoadLocalResources();
        void LoadBaseResources();
        Sprite GetNoSprite();
    }
}