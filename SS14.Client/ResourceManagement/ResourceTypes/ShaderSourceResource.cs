using System.Collections.Generic;
using System.IO;
using System.Text;
using SS14.Client.Graphics.Shaders;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Shared.Utility;

namespace SS14.Client.ResourceManagement.ResourceTypes
{
    /// <summary>
    ///     Loads the **source code** of a shader.
    /// </summary>
    public class ShaderSourceResource : BaseResource
    {
        internal Godot.Shader GodotShader { get; private set; }

        private Dictionary<string, ShaderParamType> Parameters = new Dictionary<string, ShaderParamType>();

        public override void Load(IResourceCache cache, ResourcePath path)
        {
            using (var stream = cache.ContentFileRead(path))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                var code = reader.ReadToEnd();
                GodotShader = new Godot.Shader
                {
                    Code = code,
                };
            }
        }
    }
}
