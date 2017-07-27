using SFML.Graphics;
using SS14.Client.Graphics.Shader;
using SS14.Shared.GameObjects;
using System.Collections.Generic;
using SS14.Client.ResourceManagement;

namespace SS14.Client.Interfaces.Resource
{
    public interface IResourceCache
    {
        Dictionary<Texture, string> TextureToKey { get; }
        Sprite GetSprite(string key);
        bool SpriteExists(string key);
        GLSLShader GetShader(string key);
        TechniqueList GetTechnique(string key);
        ParticleSettings GetParticles(string key);
        object GetAnimatedSprite(string key);
        void LoadLocalResources();
        void LoadBaseResources();


        Sprite DefaultSprite();
        
        T GetResource<T>(string path)
            where T : BaseResource, new();

        bool TryGetResource<T>(string path, out T resource)
            where T : BaseResource, new();
    }
}
