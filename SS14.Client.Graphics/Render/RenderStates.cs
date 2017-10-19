using SS14.Client.Graphics.Shader;
using SRenderStates = SFML.Graphics.RenderStates;
using SBlendMode = SFML.Graphics.BlendMode;

namespace SS14.Client.Graphics.Render
{
    public struct RenderStates
    {
        internal SRenderStates SFMLRenderStates { get; }

        internal RenderStates(SRenderStates states)
        {
            SFMLRenderStates = states;
        }

        public RenderStates(GLSLShader shader)
        {
            if (shader == null)
            {
                SFMLRenderStates = Default.SFMLRenderStates;
            }
            else
            {
                SFMLRenderStates = new SRenderStates(shader.SFMLShader);
            }
        }

        public RenderStates(BlendMode mode) : this(new SRenderStates((SBlendMode)mode))
        {
        }

        public static readonly RenderStates Default = new RenderStates(SRenderStates.Default);
    }
}
