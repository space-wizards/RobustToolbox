using System.Collections.Generic;

namespace SS14.Client.Graphics.Shader
{
    /// <para>
    /// With the transition from FX to GLSL, GLSL shaders cannot do multiple techniques per shader,
    /// to fix that, each Technique has been converted into a standalone shader.
    /// This class combines those individual shaders into a technique list.
    /// </para>
    /// <summary>
    /// Creates a Dictionary to combine individual shaders into a list of techniques
    /// </summary>
    public class TechniqueList
    {
        private Dictionary<string, IGLSLShader> _techniqueList;

        public TechniqueList()
        {
            _techniqueList = new Dictionary<string, IGLSLShader>();
        }

        public void Add(IGLSLShader Shader)
        {
            _techniqueList.Add(Shader.ResourceName, Shader);
        }

        public IGLSLShader getShader(string ShaderName)
        {
            return _techniqueList[ShaderName];
        }

        public IGLSLShader this[string key] => _techniqueList[key.ToLowerInvariant()];

        public Dictionary<string, IGLSLShader> Dictonary => _techniqueList;

        public string Name
        {
            get;
            set;
        }
    }
}
