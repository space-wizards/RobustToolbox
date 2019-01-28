using SS14.Client.Interfaces.Graphics;
using SS14.Shared.IoC;
using SS14.Shared.Maths;

namespace SS14.Client.Input
{
    internal class OpenGLInputManager : InputManager
    {
        [Dependency] private readonly IDisplayManagerOpenGL _displayManagerOpenGL;

        public override Vector2 MouseScreenPosition => _displayManagerOpenGL.MouseScreenPosition;
    }
}
