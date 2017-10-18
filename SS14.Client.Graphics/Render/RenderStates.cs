using SS14.Client.Graphics.Shader;
using SRenderStates = SFML.Graphics.RenderStates;

namespace SS14.Client.Graphics.Render
{
    public struct RenderStates
    {
        internal SRenderStates SFMLRenderStates { get; }

        internal RenderStates(SRenderStates states)
        {
            SFMLRenderStates = states;
        }

        public RenderStates(GLSLShader shader) : this(new SRenderStates(shader.SFMLShader))
        {
        }

        public static readonly RenderStates Default = new RenderStates(SRenderStates.Default);
    }
}
