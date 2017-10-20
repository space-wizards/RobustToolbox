using SS14.Client.Graphics.Shader;
using SS14.Client.Graphics.Sprites;
using SS14.Client.Graphics.Textures;
using SS14.Client.ResourceManagement;
using SS14.Shared.GameObjects;
using System.Collections.Generic;
using System.IO;

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
        AnimatedSprite GetAnimatedSprite(string key);
        void LoadLocalResources();
        void LoadBaseResources();
        /// <summary>
        /// Load a texture from the stream and register it under the specified name.
        /// </summary>
        /// <param name="name">The name to register the texture under.</param>
        /// <param name="stream">The stream to read from. Note that this stream must support seeking!</param>
        /// <returns>The loaded texture.</returns>
        Texture LoadTextureFrom(string name, Stream stream);
        Sprite LoadSpriteFromTexture(string name, Texture texture);


        Sprite DefaultSprite();

        T GetResource<T>(string path)
            where T : BaseResource, new();

        bool TryGetResource<T>(string path, out T resource)
            where T : BaseResource, new();

        void CacheResource<T>(string path, T resource)
            where T : BaseResource, new();
    }
}
